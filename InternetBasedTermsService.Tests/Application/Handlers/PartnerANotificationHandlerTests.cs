

using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using InternetBasedTermsService.Application.Handlers;
using InternetBasedTermsService.Infrastructure;

namespace InternetBasedTermsService.Tests.Application.Handlers;

public class PartnerANotificationHandlerTests
{
    private readonly Mock<ILogger<PartnerANotificationHandler>> _mockLogger;
    private readonly PartnerANotificationHandler _handler;

    public PartnerANotificationHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PartnerANotificationHandler>>();
        _handler = new PartnerANotificationHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldLogExpectedInformation()
    {
        // Arrange
        var notification = new IbtDataProcessedNotification(
            "EventA", "Product A Full Name", "TypeA", "ISINA",
            DateTime.UtcNow, Guid.NewGuid());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        // Verify that specific log messages were made. This can be brittle.
        // A simpler assertion might be to ensure the handler completes without error.
        // For demonstration, checking one log message part:
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(notification.ProductNameFull)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

         _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(notification.IbtTypeCode)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        // Add more checks for EventType, ISIN if detailed log checking is desired.
    }
}