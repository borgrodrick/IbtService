using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Application.Handlers;
using InternetBasedTermsService.Application.Notifications;

namespace InternetBasedTermsService.Tests.Application.Handlers;
using Xunit;
using Moq;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class IbtDataProcessingOrchestratorTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly IbtDataProcessingOrchestrator _orchestrator;

        public IbtDataProcessingOrchestratorTests()
        {
            _mockMediator = new Mock<IMediator>();
            var mockLogger = new Mock<ILogger<IbtDataProcessingOrchestrator>>();
            _orchestrator = new IbtDataProcessingOrchestrator(_mockMediator.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_IbtDataProcessedNotification_ShouldSendCommandForEachAction()
        {
            // Arrange
            var notification = new IbtDataProcessedNotification(
                EventType: "9097",
                ProductNameFull: "Test Product",
                IbtTypeCode: "T123",
                Isin: "ISINXYZ",
                ProcessingTimestamp: DateTime.UtcNow,
                CorrelationId: Guid.NewGuid()
            );

            LogEventCommand? sentLogCommand = null;
            NotifyPartnerACommand? sentPartnerACommand = null;
            ProcessPartnerBDataCommand? sentPartnerBCommand = null;

            _mockMediator.Setup(m => m.Send(It.IsAny<LogEventCommand>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest, CancellationToken>((cmd, ct) => sentLogCommand = cmd as LogEventCommand)
                .Returns(Task.CompletedTask);

            _mockMediator.Setup(m => m.Send(It.IsAny<NotifyPartnerACommand>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest, CancellationToken>((cmd, ct) => sentPartnerACommand = cmd as NotifyPartnerACommand)
                .Returns(Task.CompletedTask);

            _mockMediator.Setup(m => m.Send(It.IsAny<ProcessPartnerBDataCommand>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest, CancellationToken>((cmd, ct) => sentPartnerBCommand = cmd as ProcessPartnerBDataCommand)
                .Returns(Task.CompletedTask);

            // Act
            await _orchestrator.Handle(notification, CancellationToken.None);

            // Assert
            // Verify Send was called for each command type
            _mockMediator.Verify(m => m.Send(It.IsAny<LogEventCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Send(It.IsAny<NotifyPartnerACommand>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Send(It.IsAny<ProcessPartnerBDataCommand>(), It.IsAny<CancellationToken>()), Times.Once);

            // Verify the content of the commands (optional, but good for important fields)
            sentLogCommand.Should().NotBeNull();
            sentLogCommand?.EventType.Should().Be(notification.EventType);
            sentLogCommand?.CorrelationId.Should().Be(notification.CorrelationId);
            sentLogCommand?.Timestamp.Should().Be(notification.ProcessingTimestamp);

            sentPartnerACommand.Should().NotBeNull();
            sentPartnerACommand?.ProductNameFull.Should().Be(notification.ProductNameFull);
            sentPartnerACommand?.IbtTypeCode.Should().Be(notification.IbtTypeCode);
            sentPartnerACommand?.EventType.Should().Be(notification.EventType);
            sentPartnerACommand?.Isin.Should().Be(notification.Isin);
            sentPartnerACommand?.CorrelationId.Should().Be(notification.CorrelationId);

            sentPartnerBCommand.Should().NotBeNull();
            sentPartnerBCommand?.EventType.Should().Be(notification.EventType);
            sentPartnerBCommand?.Isin.Should().Be(notification.Isin);
            sentPartnerBCommand?.ProcessingTimestamp.Should().Be(notification.ProcessingTimestamp);
            sentPartnerBCommand?.CorrelationId.Should().Be(notification.CorrelationId);
        }
    }