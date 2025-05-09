using InternetBasedTermsService.Infrastructure;
using MediatR;

namespace InternetBasedTermsService.Application.Handlers;

public class DbLoggingNotificationHandler(
    ILogger<DbLoggingNotificationHandler> logger,
    DatabaseLoggerSimulator dbLogger)
    : INotificationHandler<IbtDataProcessedNotification>
{
    public Task Handle(IbtDataProcessedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "HANDLER [DbLoggingNotification]: Processing for CorrelationId: {CorrelationId}. Logging EventType: {EventType}",
            notification.CorrelationId, notification.EventType);

        dbLogger.LogEvent(notification.EventType, notification.ProcessingTimestamp);
        return Task.CompletedTask;
    }
}
