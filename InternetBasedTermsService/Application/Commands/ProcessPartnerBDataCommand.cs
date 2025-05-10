namespace InternetBasedTermsService.Application.Commands;

public record ProcessPartnerBDataCommand(
    string EventType,
    string Isin,
    DateTime ProcessingTimestamp,
    Guid CorrelationId) : ICorrelatedRequest;