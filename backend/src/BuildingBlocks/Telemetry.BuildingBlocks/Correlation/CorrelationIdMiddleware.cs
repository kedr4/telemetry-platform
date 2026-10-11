using Microsoft.AspNetCore.Http;

namespace Telemetry.BuildingBlocks.Correlation;

/// <summary>
/// Reads the correlation identifier from the request (or creates one), exposes it to the rest of the
/// pipeline through <see cref="CorrelationContext"/> and echoes it in the response.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationContext.HeaderName].ToString();
        var correlationId = CorrelationContext.IsValid(incoming) ? incoming : CorrelationContext.NewId();

        context.Response.Headers[CorrelationContext.HeaderName] = correlationId;

        using (CorrelationContext.BeginScope(correlationId))
        {
            await next(context);
        }
    }
}
