using InternetBasedTermsService.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InternetBasedTermsService.Tests.Helper;

public class TestableNotifyPartnerACommandHandler(ILogger<TestableNotifyPartnerACommandHandler> logger)
    : IRequestHandler<NotifyPartnerACommand>
{
    internal int HandleCallCount { get; set; } = 0;
    public NotifyPartnerACommand? LastReceivedCommand { get; private set; }

    public Task Handle(NotifyPartnerACommand request, CancellationToken cancellationToken)
    {
        HandleCallCount++;
        LastReceivedCommand = request;
        logger.LogInformation("[TestableNotifyPartnerACommandHandler]: Received command for CorrelationId {CorrelationId}", request.CorrelationId);
        return Task.CompletedTask;
    }
}