namespace InternetBasedTermsService.Application.Interfaces;

public interface IDatabaseLogger
{
        // In a real scenario, this might be async Task LogEventAsync(...)
        void LogEvent(string eventType, DateTime timestamp);
}