using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Application.Interfaces;
using MediatR;

namespace InternetBasedTermsService.Application.Handlers;

public class LogEventCommandHandler( ILogger<LogEventCommandHandler> logger, IDatabaseLogger dbLogger) : IRequestHandler<LogEventCommand>
{
    public Task Handle(LogEventCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("LogEventCommandHandler: Persisting event type {EventType} for CorrelationId {CorrelationId}",
            request.EventType, request.CorrelationId);
        dbLogger.LogEvent(request.EventType, request.Timestamp);
        return Task.CompletedTask;
    }
}
