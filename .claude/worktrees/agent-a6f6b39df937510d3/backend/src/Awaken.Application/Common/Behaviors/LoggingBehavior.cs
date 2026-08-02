using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;

        logger.LogInformation("Handling {RequestName} {CorrelationId}", requestName, correlationId);

        try
        {
            var response = await next(cancellationToken);
            logger.LogInformation("Handled {RequestName} {CorrelationId}", requestName, correlationId);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling {RequestName} {CorrelationId}", requestName, correlationId);
            throw;
        }
    }
}
