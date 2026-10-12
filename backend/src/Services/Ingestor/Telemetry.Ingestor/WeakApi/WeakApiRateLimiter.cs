using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Telemetry.Ingestor.WeakApi;

/// <summary>
/// Owns the single client-side rate limiter shared by all WeakApp requests, so that the DI container
/// disposes it together with the application. <see cref="Limiter"/> is <c>null</c> when limiting is disabled.
/// </summary>
internal sealed class WeakApiRateLimiter : IDisposable
{
    public WeakApiRateLimiter(IOptions<WeakApiOptions> options)
    {
        var settings = options.Value.RateLimit;
        Limiter = settings.PermitLimit == 0
            ? null
            : new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = settings.Window,
                SegmentsPerWindow = 8,
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    }

    public RateLimiter? Limiter { get; }

    public void Dispose() => Limiter?.Dispose();
}
