using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Infrastructure;
using MediatR;

namespace InternetBasedTermsService.Application.Handlers;

public class NotifyPartnerACommandHandler (ILogger<NotifyPartnerACommandHandler > logger)
    : IRequestHandler<NotifyPartnerACommand >
{
    public Task Handle(NotifyPartnerACommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("--- Email to Partner A (via Command Handler) ---");
        logger.LogInformation("   CorrelationId: {CorrelationId}", request.CorrelationId);
        logger.LogInformation("   ProductNameFull: {ProductNameFull}", request.ProductNameFull);
        logger.LogInformation("   IBTTypeCode: {IBTTypeCode}", request.IbtTypeCode);
        logger.LogInformation("   EventType: {EventType}", request.EventType);
        logger.LogInformation("   ISIN: {ISIN}", request.Isin);
        logger.LogInformation("--- End Email Simulation ---");
        return Task.CompletedTask;
    }
}