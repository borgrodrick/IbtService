using MediatR;

namespace InternetBasedTermsService.Application.Commands;

public interface ICorrelatedRequest : IRequest 
{
    Guid CorrelationId { get; }
}