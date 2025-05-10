namespace InternetBasedTermsService.Application.Commands;

public record NotifyPartnerACommand(
    string ProductNameFull,
    string IbtTypeCode,
    string EventType,
    string Isin,
    Guid CorrelationId) : ICorrelatedRequest;