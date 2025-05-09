using InternetBasedTermsService.Infrastructure;
using MediatR;

namespace InternetBasedTermsService.Application.Handlers;

public class PartnerANotificationHandler(ILogger<PartnerANotificationHandler> logger)
    : INotificationHandler<IbtDataProcessedNotification>
{
    public Task Handle(IbtDataProcessedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "HANDLER [PartnerANotification]: Processing for CorrelationId: {CorrelationId}. Simulating email.",
            notification.CorrelationId);

        // Simulate sending email
        logger.LogInformation("--- Email to Partner A (via MediatR Notification Handler) ---");
        logger.LogInformation("   CorrelationId: {CorrelationId}", notification.CorrelationId);
        logger.LogInformation("   ProductNameFull: {ProductNameFull}", notification.ProductNameFull);
        logger.LogInformation("   IBTTypeCode: {IBTTypeCode}", notification.IbtTypeCode);
        logger.LogInformation("   EventType: {EventType}", notification.EventType);
        logger.LogInformation("   ISIN: {ISIN}", notification.Isin);
        logger.LogInformation("--- End Email Simulation ---");

        return Task.CompletedTask;
    }
}