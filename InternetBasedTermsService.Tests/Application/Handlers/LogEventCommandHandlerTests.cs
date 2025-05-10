using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Application.Handlers;
using InternetBasedTermsService.Application.Interfaces;
using InternetBasedTermsService.Infrastructure.Persistence;


namespace InternetBasedTermsService.Tests.Application.Handlers;

public class LogEventCommandHandlerTests
{
   private readonly Mock<IDatabaseLogger> _mockDbLogger;
    private readonly Mock<ILogger<LogEventCommandHandler>> _mockHandlerLogger;
    private readonly LogEventCommandHandler _handler;

    public LogEventCommandHandlerTests()
    {
        _mockDbLogger = new Mock<IDatabaseLogger>();
        _mockHandlerLogger = new Mock<ILogger<LogEventCommandHandler>>();
        _handler = new LogEventCommandHandler(_mockHandlerLogger.Object, _mockDbLogger.Object);
    }

    [Fact]
    public async Task Handle_LogEventCommand_ShouldCallDbLoggerLogEvent_WithCorrectParameters()
    {
        // Arrange
        var command = new LogEventCommand(
            EventType: "TestEventOccurred",
            Timestamp: DateTime.UtcNow,
            CorrelationId: Guid.NewGuid()
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Verify that the LogEvent method on the mocked IDatabaseLogger was called once
        // with the exact EventType and Timestamp from the command.
        _mockDbLogger.Verify(
            db => db.LogEvent(command.EventType, command.Timestamp),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogEventCommand_WhenDbLoggerThrowsException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var command = new LogEventCommand(
            EventType: "EventWithError",
            Timestamp: DateTime.UtcNow,
            CorrelationId: Guid.NewGuid()
        );
        var dbException = new InvalidOperationException("Database unavailable");

        _mockDbLogger.Setup(db => db.LogEvent(command.EventType, command.Timestamp))
                     .Throws(dbException);

        // Act & Assert
        await FluentActions.Awaiting(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database unavailable");
    }
}
