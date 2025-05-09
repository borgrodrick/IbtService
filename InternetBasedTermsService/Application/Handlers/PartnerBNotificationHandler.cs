using System.Xml.Linq;
using InternetBasedTermsService.Infrastructure;
using MediatR;

namespace InternetBasedTermsService.Application.Handlers;

public class PartnerBNotificationHandler(ILogger<PartnerBNotificationHandler> logger)
    : INotificationHandler<IbtDataProcessedNotification>
{
    private const string RequiredEventType = "9097";
    private const string OutputFileName = "InstrumentNotification.xml"; // Consider making configurable

    public Task Handle(IbtDataProcessedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "HANDLER [PartnerBNotification]: Processing for CorrelationId: {CorrelationId}. EventType: {EventType}",
            notification.CorrelationId, notification.EventType);

        if (notification.EventType == RequiredEventType)
        {
            logger.LogInformation("PartnerB Handler: EventType matches. Processing...");

            var timestampString = notification.ProcessingTimestamp.ToString("o"); // Use timestamp from notification

            if (!string.IsNullOrEmpty(notification.Isin))
            {
                var outputDoc = new XDocument(
                    new XElement("InstrumentNotification",
                        new XElement("Timespan", timestampString),
                        new XElement("ISIN", notification.Isin)
                    )
                );
                try
                {
                    outputDoc.Save(OutputFileName);
                    logger.LogInformation(
                        "PartnerB Handler: Successfully created '{FileName}' for CorrelationId: {CorrelationId}",
                        OutputFileName, notification.CorrelationId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "PartnerB Handler: Error saving XML file '{FileName}' for CorrelationId: {CorrelationId}",
                        OutputFileName, notification.CorrelationId);
                    // Depending on requirements, you might want to let the exception propagate
                    // or handle it gracefully here if it's not critical for other handlers.
                }
            }
            else
            {
                logger.LogWarning(
                    "PartnerB Handler: Skipping file creation - ISIN is missing. CorrelationId: {CorrelationId}",
                    notification.CorrelationId);
            }
        }
        else
        {
            logger.LogInformation(
                "PartnerB Handler: Skipping - EventType '{ActualEventType}' doesn't match required '{RequiredEventType}'. CorrelationId: {CorrelationId}",
                notification.EventType, RequiredEventType, notification.CorrelationId);
        }
        return Task.CompletedTask;
    }
}