using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using InternetBasedTermsService.Application.Handlers;
using InternetBasedTermsService.Infrastructure;

// Assuming DatabaseLoggerSimulator is in the main project

namespace InternetBasedTermsService.Tests.Application.Handlers;

public class DbLoggingNotificationHandlerTests
{
    private readonly Mock<DatabaseLoggerSimulator> _mockDbLoggerSimulator;
    private readonly Mock<ILogger<DbLoggingNotificationHandler>> _mockLogger;
    private readonly DbLoggingNotificationHandler _handler;

    public DbLoggingNotificationHandlerTests()
    {
        _mockDbLoggerSimulator = new Mock<DatabaseLoggerSimulator>(Mock.Of<ILogger<DatabaseLoggerSimulator>>()); // DbLogger itself has a logger
        _mockLogger = new Mock<ILogger<DbLoggingNotificationHandler>>();
        _handler = new DbLoggingNotificationHandler(_mockLogger.Object, _mockDbLoggerSimulator.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallLogEvent_WithCorrectParameters()
    {
        // Arrange
        var notification = new IbtDataProcessedNotification(
            "TestEvent", "ProdName", "TypeCode", "ISIN123",
            DateTime.UtcNow, Guid.NewGuid());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockDbLoggerSimulator.Verify(
            logger => logger.LogEvent(notification.EventType, notification.ProcessingTimestamp),
            Times.Once);
    }
}