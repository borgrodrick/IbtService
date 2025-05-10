using System.Xml.Linq;
using InternetBasedTermsService.Application.Commands;
using MediatR;

namespace InternetBasedTermsService.Application.Handlers;

public class ProcessPartnerBDataCommandHandler(ILogger<ProcessPartnerBDataCommandHandler> logger)
    :IRequestHandler<ProcessPartnerBDataCommand>
{
    private const string RequiredEventType = "9097";
    private const string OutputFileName = "InstrumentNotification.xml";

    public Task Handle(ProcessPartnerBDataCommand request, CancellationToken cancellationToken)
    {
        if (request.EventType == RequiredEventType)
        {
            logger.LogDebug("ProcessPartnerBDataCommandHandler: EventType matches for CorrelationId {CorrelationId}. Processing...",
                request.CorrelationId);

            var timestampString = request.ProcessingTimestamp.ToString("o");

            if (!string.IsNullOrEmpty(request.Isin))
            {
                var outputDoc = new XDocument(
                    new XElement("InstrumentNotification",
                        new XElement("Timespan", timestampString),
                        new XElement("ISIN", request.Isin)
                    )
                );
                try
                {
                    outputDoc.Save(OutputFileName); // Consider configurable path
                    logger.LogInformation(
                        "ProcessPartnerBDataCommandHandler: Successfully created '{FileName}' for CorrelationId: {CorrelationId}",
                        OutputFileName, request.CorrelationId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "ProcessPartnerBDataCommandHandler: Error saving XML file '{FileName}' for CorrelationId: {CorrelationId}",
                        OutputFileName, request.CorrelationId);
                    throw; // Allow pipeline to handle/log if needed, or retry logic
                }
            }
            else
            {
                logger.LogWarning(
                    "ProcessPartnerBDataCommandHandler: Skipping file creation for CorrelationId {CorrelationId} - ISIN is missing.",
                    request.CorrelationId);
            }
        }
        else
        {
             logger.LogDebug(
                "ProcessPartnerBDataCommandHandler: Skipping for CorrelationId {CorrelationId} - EventType '{ActualEventType}' doesn't match required '{RequiredEventType}'.",
                request.CorrelationId, request.EventType, RequiredEventType);
        }
        return Task.CompletedTask;
    }
}