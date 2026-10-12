using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Timeout;

namespace Telemetry.Ingestor.WeakApi;

/// <summary>
/// HTTP resilience pipeline for the upstream API, from the outside in:
/// total timeout → retry (exponential back-off with jitter) → circuit breaker → rate limiter → per-attempt timeout.
/// The rate limiter sits inside the retry so that every real HTTP attempt consumes a permit.
/// Only transient failures (5xx, 408, timeouts, transport errors, truncated bodies) are retried or counted by the breaker.
/// 429 is deliberately excluded: retrying immediately would only extend the rate-limit window, so the
/// ingestion loop backs off for the advertised <c>Retry-After</c> instead.
/// </summary>
internal static partial class WeakApiResilience
{
    public const string PipelineName = "weak-api";

    public static void Configure(
        ResiliencePipelineBuilder<HttpResponseMessage> pipeline,
        WeakApiOptions options,
        RateLimiter? rateLimiter,
        ILogger logger)
    {
        pipeline
            .AddTimeout(options.TotalTimeout)
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = Math.Max(options.MaxRetryAttempts, 1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = options.RetryBaseDelay,
                ShouldRetryAfterHeader = false,
                ShouldHandle = args => ValueTask.FromResult(
                    options.MaxRetryAttempts > 0 && IsTransientFailure(args.Outcome)),
                OnRetry = args =>
                {
                    LogRetry(logger, args.AttemptNumber + 1, Describe(args.Outcome), args.RetryDelay);
                    return ValueTask.CompletedTask;
                },
            })
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitBreaker.FailureRatio,
                MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                SamplingDuration = options.CircuitBreaker.SamplingDuration,
                BreakDuration = options.CircuitBreaker.BreakDuration,
                ShouldHandle = args => ValueTask.FromResult(IsTransientFailure(args.Outcome)),
                OnOpened = args =>
                {
                    LogCircuitOpened(logger, args.BreakDuration);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    LogCircuitClosed(logger);
                    return ValueTask.CompletedTask;
                },
            });

        if (rateLimiter is not null)
        {
            pipeline.AddRateLimiter(rateLimiter);
        }

        pipeline.AddTimeout(options.AttemptTimeout);
    }

    internal static bool IsTransientFailure(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Exception: HttpRequestException or IOException or TimeoutRejectedException } => true,
        { Result.StatusCode: HttpStatusCode.RequestTimeout } => true,
        { Result.StatusCode: var status } when (int)status >= 500 => true,
        _ => false,
    };

    private static string Describe(Outcome<HttpResponseMessage> outcome) =>
        outcome.Exception is { } exception
            ? exception.GetType().Name
            : $"HTTP {(int)outcome.Result!.StatusCode}";

    [LoggerMessage(Level = LogLevel.Warning, Message = "WeakApp request failed ({Failure}); retry {Attempt} in {Delay}")]
    private static partial void LogRetry(ILogger logger, int attempt, string failure, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Error, Message = "WeakApp circuit opened for {BreakDuration} after repeated failures")]
    private static partial void LogCircuitOpened(ILogger logger, TimeSpan breakDuration);

    [LoggerMessage(Level = LogLevel.Information, Message = "WeakApp circuit closed; requests resume")]
    private static partial void LogCircuitClosed(ILogger logger);
}
