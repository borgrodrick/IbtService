using InternetBasedTermsService.Application.Notifications;
using InternetBasedTermsService.Application.Parsing;
using MediatR;

namespace InternetBasedTermsService.Infrastructure.Workers;

public class IngestionWorker(
    ILogger<IngestionWorker> logger,
    XmlParser parser,
    IMediator mediator, // Changed from IBus
    IConfiguration configuration)
    : BackgroundService
{

    private readonly string _ibtFilePath = configuration.GetValue<string>("InputFilePath") ?? "IBT.xml";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IngestionWorker running at: {time}", DateTimeOffset.Now);
        logger.LogInformation("Attempting to process file: {FilePath}", _ibtFilePath);

        if (!File.Exists(_ibtFilePath))
        {
            logger.LogError("Input file not found: {FilePath}. Worker will stop.", _ibtFilePath);
            return;
        }

        var parsedData = parser.ParseFromFile(_ibtFilePath);

        if (parsedData != null && !stoppingToken.IsCancellationRequested)
        {
            var correlationId = Guid.NewGuid(); // Generate a correlation ID for this processing event
            var processingTimestamp = DateTime.UtcNow;

            logger.LogInformation(
                "Successfully parsed IBT data. Publishing IbtDataProcessedNotification with CorrelationId: {CorrelationId}",
                correlationId);

            // Create the notification
            var notification = new IbtDataProcessedNotification(
                parsedData.EventType,
                parsedData.ProductNameFull,
                parsedData.IbtTypeCode,
                parsedData.Isin,
                processingTimestamp,
                correlationId
            );

            // Publish the notification via MediatR
            // MediatR will dispatch this to all registered INotificationHandler<IbtDataProcessedNotification>
            await mediator.Publish(notification, stoppingToken);

            logger.LogInformation("IbtDataProcessedNotification published successfully for CorrelationId: {CorrelationId}.", correlationId);
        }
        else if (parsedData == null)
        {
            logger.LogError("Failed to parse IBT data from {FilePath}. No notification published.", _ibtFilePath);
        }

        logger.LogInformation("IngestionWorker finished its current processing cycle.");
        // For this exercise, it runs once. It could be modified to watch a directory.
    }
}