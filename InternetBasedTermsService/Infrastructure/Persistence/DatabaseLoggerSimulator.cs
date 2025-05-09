namespace InternetBasedTermsService.Application.Handlers;

public class DatabaseLoggerSimulator(ILogger<DatabaseLoggerSimulator> logger)
{
    public void LogEvent(string eventType, DateTime timestamp)
    {
        logger.LogInformation("DATABASE LOG SIMULATION: EventType='{EventType}', Timestamp='{Timestamp}'",
            eventType, timestamp.ToString("o"));
    }
}