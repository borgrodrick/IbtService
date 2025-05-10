using InternetBasedTermsService.Application.Interfaces;

namespace InternetBasedTermsService.Infrastructure.Persistence;

public class DatabaseLoggerSimulator(ILogger<DatabaseLoggerSimulator> logger) : IDatabaseLogger
{
    public void LogEvent(string eventType, DateTime timestamp)
    {
        logger.LogInformation("DATABASE LOG SIMULATION: EventType='{EventType}', Timestamp='{Timestamp}'",
            eventType, timestamp.ToString("o"));
    }
}