using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Spim.Application.Common.Interfaces;

namespace Spim.Application.Common.Behaviours;

public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} for user {UserId}", requestName, currentUser.UserId ?? "anonymous");

        var start = Stopwatch.GetTimestamp();
        var response = await next(cancellationToken);

        logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds:0.0} ms",
            requestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);

        return response;
    }
}
