using Moq;
using Microsoft.Extensions.Logging;
using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Application.Handlers;

namespace InternetBasedTermsService.Tests.Application.Handlers;

 public class NotifyPartnerACommandHandlerTests
    {
        private readonly Mock<ILogger<NotifyPartnerACommandHandler>> _mockLogger;
        private readonly NotifyPartnerACommandHandler _handler;

        public NotifyPartnerACommandHandlerTests()
        {
            _mockLogger = new Mock<ILogger<NotifyPartnerACommandHandler>>();
            _handler = new NotifyPartnerACommandHandler(_mockLogger.Object);
        }

        [Fact]
        public async Task Handle_NotifyPartnerACommand_ShouldLogCorrectInformation()
        {
            // Arrange
            var command = new NotifyPartnerACommand(
                ProductNameFull: "Super Product X",
                IbtTypeCode: "SPX001",
                EventType: "SALE",
                Isin: "ISINPROD001",
                CorrelationId: Guid.NewGuid()
            );

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Verify that specific log messages were made.
            // This checks if the logger was called with a message containing the product name.
            VerifyLogContains(_mockLogger, LogLevel.Information, command.ProductNameFull);
            VerifyLogContains(_mockLogger, LogLevel.Information, command.IbtTypeCode);
            VerifyLogContains(_mockLogger, LogLevel.Information, command.EventType);
            VerifyLogContains(_mockLogger, LogLevel.Information, command.Isin);
            VerifyLogContains(_mockLogger, LogLevel.Information, command.CorrelationId.ToString());
        }

        // Helper method to verify logging with specific content
        private static void VerifyLogContains<T>(Mock<ILogger<T>> loggerMock, LogLevel level, string expectedMessagePart)
        {
            loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessagePart)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce); // Use AtLeastOnce if the part appears in multiple log lines or once if specific
        }
    }