using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Telemetry.Gateway.Application.Behaviors;

/// <summary>Logs how long each query takes, and warns about slow ones.</summary>
internal sealed partial class QueryLoggingBehavior<TRequest, TResponse>(ILogger<QueryLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan SlowQueryThreshold = TimeSpan.FromSeconds(1);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await next(cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started);

        if (elapsed >= SlowQueryThreshold)
        {
            LogSlowQuery(typeof(TRequest).Name, elapsed.TotalMilliseconds);
        }
        else
        {
            LogQuery(typeof(TRequest).Name, elapsed.TotalMilliseconds);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Query} completed in {ElapsedMs:0.0} ms")]
    private partial void LogQuery(string query, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Slow query: {Query} took {ElapsedMs:0.0} ms")]
    private partial void LogSlowQuery(string query, double elapsedMs);
}
