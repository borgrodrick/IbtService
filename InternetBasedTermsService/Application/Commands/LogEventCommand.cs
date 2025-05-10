namespace InternetBasedTermsService.Application.Commands;

public record LogEventCommand(
    string EventType,
    DateTime Timestamp,
    Guid CorrelationId) : ICorrelatedRequest;