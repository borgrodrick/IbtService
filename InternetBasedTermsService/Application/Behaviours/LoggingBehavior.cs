using System.Diagnostics;
using InternetBasedTermsService.Application.Commands;
using MediatR;

namespace InternetBasedTermsService.Application.Behaviours;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = Guid.Empty; // Default

        // Try to get CorrelationId if request implements ICorrelatedRequest or has the property
        if (request is ICorrelatedRequest correlatedRequest) // Check for interface first
        {
            correlationId = correlatedRequest.CorrelationId;
        }
        
        logger.LogInformation(
            "[PIPELINE] Handling {RequestName}. CorrelationId: {CorrelationId}. Request Data: {@Request}",
            requestName,
            correlationId,
            request); // Structured logging of the request

        var stopwatch = Stopwatch.StartNew();

        var response = await next(cancellationToken); // Call the actual command handler

        stopwatch.Stop();

        logger.LogInformation(
            "[PIPELINE] Handled {RequestName}. CorrelationId: {CorrelationId}. Execution Time: {ExecutionTime}ms.",
            requestName,
            correlationId,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}