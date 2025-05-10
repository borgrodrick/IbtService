using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Application.Notifications;
using MediatR;

namespace InternetBasedTermsService.Application.Handlers;

public class IbtDataProcessingOrchestrator(IMediator mediator, ILogger<IbtDataProcessingOrchestrator> logger)
    : INotificationHandler<IbtDataProcessedNotification>
{
    public async Task Handle(IbtDataProcessedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "ORCHESTRATOR: Received IbtDataProcessedNotification for CorrelationId {CorrelationId}. Dispatching specific commands.",
            notification.CorrelationId);

        // Dispatch Log Event Command
        var logCommand = new LogEventCommand(
            notification.EventType,
            notification.ProcessingTimestamp,
            notification.CorrelationId);
        await mediator.Send(logCommand, cancellationToken);

        // Dispatch Partner A Command
        var partnerACommand = new NotifyPartnerACommand(
            notification.ProductNameFull,
            notification.IbtTypeCode,
            notification.EventType,
            notification.Isin,
            notification.CorrelationId);
        await mediator.Send(partnerACommand, cancellationToken);

        // Dispatch Partner B Command
        var partnerBCommand = new ProcessPartnerBDataCommand(
            notification.EventType,
            notification.Isin,
            notification.ProcessingTimestamp,
            notification.CorrelationId);
        await mediator.Send(partnerBCommand, cancellationToken);

        logger.LogInformation(
            "ORCHESTRATOR: Finished dispatching commands for CorrelationId {CorrelationId}.",
            notification.CorrelationId);
    }
}