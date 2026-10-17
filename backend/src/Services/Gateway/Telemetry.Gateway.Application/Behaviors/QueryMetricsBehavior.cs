using System.Diagnostics;
using MediatR;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Application.Diagnostics;

namespace Telemetry.Gateway.Application.Behaviors;

/// <summary>Records the duration and outcome of every query.</summary>
internal sealed class QueryMetricsBehavior<TRequest, TResponse>(GatewayMetrics metrics)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var started = Stopwatch.GetTimestamp();
        var outcome = "error";
        try
        {
            var response = await next(cancellationToken);
            outcome = "success";
            return response;
        }
        catch (RequestValidationException)
        {
            outcome = "invalid";
            throw;
        }
        finally
        {
            metrics.RecordQuery(typeof(TRequest).Name, outcome, Stopwatch.GetElapsedTime(started));
        }
    }
}
