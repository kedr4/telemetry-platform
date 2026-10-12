using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telemetry.Ingestor.IntegrationTests.Infrastructure;
using Telemetry.Ingestor.WeakApi;

namespace Telemetry.Ingestor.IntegrationTests.WeakApi;

/// <summary>
/// Exercises the real HTTP client and resilience pipeline against a fake WeakApp.
/// Timings are scaled down so the suite stays fast.
/// </summary>
public sealed class WeakApiClientResilienceTests : IDisposable
{
    private readonly FakeWeakApp _api = new();
    private ServiceProvider? _provider;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _provider?.Dispose();
        _api.Dispose();
    }

    [Fact]
    public async Task ValidResponse_ReturnsMeters()
    {
        _api.Always(FakeWeakApp.Ok());

        var result = await CreateClient().GetMetersAsync(CancellationToken);

        result.ShouldBeOfType<FetchResult.Success>().Meters.Count.ShouldBe(4);
        _api.MeterRequests.ShouldBe(1);
    }

    [Fact]
    public async Task TransientServerErrors_AreRetriedUntilSuccess()
    {
        _api.Sequence(FakeWeakApp.Status(500, "Unknown error"), FakeWeakApp.Status(502, "Bad Gateway"), FakeWeakApp.Ok());

        var result = await CreateClient().GetMetersAsync(CancellationToken);

        result.ShouldBeOfType<FetchResult.Success>();
        _api.MeterRequests.ShouldBe(3);
    }

    [Fact]
    public async Task PersistentServerErrors_FailAfterRetries()
    {
        _api.Always(FakeWeakApp.Status(504, "Gateway Timeout"));

        var result = await CreateClient().GetMetersAsync(CancellationToken);

        result.ShouldBeOfType<FetchResult.Failed>().Reason.ShouldContain("504");
        _api.MeterRequests.ShouldBe(3); // 1 attempt + 2 retries
    }

    [Fact]
    public async Task HangingResponse_TimesOutAndIsRetried()
    {
        // The hanging response carries a different body: getting the second body proves the first attempt was abandoned.
        const string hangingBody = """[{"type":"motion","name":"Attic","payload":{"motionDetected":false}}]""";
        _api.Sequence(FakeWeakApp.Ok(hangingBody).WithDelay(TimeSpan.FromSeconds(3)), FakeWeakApp.Ok());

        var result = await CreateClient(attemptTimeout: TimeSpan.FromMilliseconds(500)).GetMetersAsync(CancellationToken);

        result.ShouldBeOfType<FetchResult.Success>().Meters.Count.ShouldBe(4);
    }

    [Fact]
    public async Task TruncatedBody_IsRetried()
    {
        // WeakApp's "data corrupted" answer is a 200 whose chunked body is aborted mid-stream.
        var handler = new TruncatingHandler(truncatedResponses: 1);

        var result = await CreateClient(primaryHandler: handler).GetMetersAsync(CancellationToken);

        result.ShouldBeOfType<FetchResult.Success>().Meters.Count.ShouldBe(4);
        handler.Requests.ShouldBe(2);
    }

    [Fact]
    public async Task PersistentlyTruncatedBody_FailsAfterRetries()
    {
        var handler = new TruncatingHandler(truncatedResponses: int.MaxValue);

        var result = await CreateClient(primaryHandler: handler).GetMetersAsync(CancellationToken);

        result.ShouldBeOfType<FetchResult.Failed>().Reason.ShouldStartWith("Transport error");
        handler.Requests.ShouldBe(3);
    }

    [Fact]
    public async Task RateLimit_IsNotRetried_AndReportsRetryAfter()
    {
        _api.Always(FakeWeakApp.RateLimited(retryAfterSeconds: 23));

        var result = await CreateClient().GetMetersAsync(CancellationToken);

        result.ShouldBe(new FetchResult.RateLimited(TimeSpan.FromSeconds(23)));
        _api.MeterRequests.ShouldBe(1);
    }

    [Fact]
    public async Task ClientSideRateLimit_HoldsRequestsBack_WithoutCallingTheApi()
    {
        _api.Always(FakeWeakApp.Ok());
        var client = CreateClient(permitLimit: 2);

        (await client.GetMetersAsync(CancellationToken)).ShouldBeOfType<FetchResult.Success>();
        (await client.GetMetersAsync(CancellationToken)).ShouldBeOfType<FetchResult.Success>();
        var third = await client.GetMetersAsync(CancellationToken);

        third.ShouldBeOfType<FetchResult.Throttled>();
        _api.MeterRequests.ShouldBe(2);
    }

    [Fact]
    public async Task CorruptedBody_IsReportedAsCorrupted()
    {
        _api.Always(FakeWeakApp.Ok("""{"error": "data corrupted"}"""));

        var result = await CreateClient().GetMetersAsync(CancellationToken);

        result.ShouldBeOfType<FetchResult.Corrupted>().Reason.ShouldContain("data corrupted");
    }

    [Fact]
    public async Task WrongApiKey_IsNotRetried()
    {
        _api.Always(FakeWeakApp.Ok());

        var result = await CreateClient(apiKey: "wrong-key").GetMetersAsync(CancellationToken);

        result.ShouldBeOfType<FetchResult.Failed>();
        _api.MeterRequests.ShouldBe(1);
    }

    [Fact]
    public async Task RepeatedFailures_OpenTheCircuit_AndStopCallingTheApi()
    {
        _api.Always(FakeWeakApp.Status(500, "Unknown error"));
        var client = CreateClient(retries: 0, circuitMinimumThroughput: 2);

        (await client.GetMetersAsync(CancellationToken)).ShouldBeOfType<FetchResult.Failed>();
        (await client.GetMetersAsync(CancellationToken)).ShouldBeOfType<FetchResult.Failed>();
        var requestsBeforeOpen = _api.MeterRequests;

        var third = await client.GetMetersAsync(CancellationToken);

        third.ShouldBeOfType<FetchResult.CircuitOpen>().RetryAfter.ShouldNotBeNull();
        _api.MeterRequests.ShouldBe(requestsBeforeOpen);
    }

    private IWeakApiClient CreateClient(
        string apiKey = FakeWeakApp.ApiKey,
        int retries = 2,
        int circuitMinimumThroughput = 100,
        TimeSpan? attemptTimeout = null,
        HttpMessageHandler? primaryHandler = null,
        int permitLimit = 1_000)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WeakApi:BaseUrl"] = _api.BaseUrl.ToString(),
                ["WeakApi:ApiKey"] = apiKey,
                ["WeakApi:AttemptTimeout"] = (attemptTimeout ?? TimeSpan.FromSeconds(2)).ToString("c", CultureInfo.InvariantCulture),
                ["WeakApi:TotalTimeout"] = "00:00:10",
                ["WeakApi:MaxRetryAttempts"] = retries.ToString(CultureInfo.InvariantCulture),
                ["WeakApi:RetryBaseDelay"] = "00:00:00.010",
                ["WeakApi:CircuitBreaker:FailureRatio"] = "0.5",
                ["WeakApi:CircuitBreaker:MinimumThroughput"] = circuitMinimumThroughput.ToString(CultureInfo.InvariantCulture),
                ["WeakApi:CircuitBreaker:SamplingDuration"] = "00:00:30",
                ["WeakApi:CircuitBreaker:BreakDuration"] = "00:00:30",
                ["WeakApi:RateLimit:PermitLimit"] = permitLimit.ToString(CultureInfo.InvariantCulture),
                ["WeakApi:RateLimit:Window"] = "00:01:00",
            })
            .Build();

        var services = new ServiceCollection()
            .AddLogging()
            .AddWeakApiClient(configuration);

        if (primaryHandler is not null)
        {
            services.AddHttpClient<IWeakApiClient, WeakApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => primaryHandler);
        }

        _provider = services.BuildServiceProvider();

        return _provider.GetRequiredService<IWeakApiClient>();
    }
}
