using MediatR;

namespace InternetBasedTermsService.Application.Notifications;

public record IbtDataProcessedNotification(
    string EventType,
    string ProductNameFull,
    string IbtTypeCode,
    string Isin,
    DateTime ProcessingTimestamp, // When the XML was processed and notification created
    Guid CorrelationId // For tracing
) : INotification;