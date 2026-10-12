using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

namespace Telemetry.Ingestor.WeakApi;

/// <summary>
/// Typed client of the upstream API. Transport-level resilience (timeouts, retries, circuit breaker)
/// is applied by the HTTP pipeline configured in <see cref="WeakApiResilience"/>; this class translates
/// what is left into a <see cref="FetchResult"/>.
/// </summary>
internal sealed class WeakApiClient(HttpClient httpClient, TimeProvider timeProvider) : IWeakApiClient
{
    public const string ApiKeyHeader = "X-Api-Key";
    public const string MetersPath = "meters";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<FetchResult> GetMetersAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(MetersPath, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new FetchResult.RateLimited(GetRetryAfter(response.Headers.RetryAfter));
            }

            if (!response.IsSuccessStatusCode)
            {
                return new FetchResult.Failed($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return Parse(body);
        }
        catch (RateLimiterRejectedException ex)
        {
            return new FetchResult.Throttled(ex.RetryAfter);
        }
        catch (BrokenCircuitException ex)
        {
            return new FetchResult.CircuitOpen(ex.RetryAfter);
        }
        catch (TimeoutRejectedException)
        {
            return new FetchResult.Failed("The request timed out.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            return new FetchResult.Failed($"Transport error: {ex.Message}");
        }
    }

    internal static FetchResult Parse(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.Array => new FetchResult.Success(
                    root.Deserialize<List<WeakApiMeterDto>>(JsonOptions) ?? []),
                JsonValueKind.Object when root.TryGetProperty("error", out var error) => new FetchResult.Corrupted(
                    $"API reported an error: {error}"),
                _ => new FetchResult.Corrupted($"Unexpected JSON {root.ValueKind}."),
            };
        }
        catch (JsonException ex)
        {
            return new FetchResult.Corrupted($"Malformed JSON: {ex.Message}");
        }
    }

    private TimeSpan? GetRetryAfter(RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var delay = date - timeProvider.GetUtcNow();
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }
}
