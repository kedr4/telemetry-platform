using System.Net;
using System.Text.Json;
using Telemetry.Gateway.IntegrationTests.Infrastructure;
using Telemetry.Testing.Metrics;

namespace Telemetry.Gateway.IntegrationTests.Api;

public sealed class RestApiTests(GatewayDatabaseFixture database) : IAsyncLifetime
{
    private GatewayApiFactory _factory = null!;
    private HttpClient _http = null!;
    private SeededHouse _house = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _house = await SeededHouse.SeedAsync(database.ConnectionString, CancellationToken);
        _factory = new GatewayApiFactory(database);
        _http = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task LatestReadings_AreSerializedPolymorphically()
    {
        using var json = await GetJsonAsync($"/api/v1/readings/latest?location={_house.Garage}");

        var readings = json.RootElement.EnumerateArray().ToList();
        readings.Count.ShouldBe(2);
        var energy = readings.Single(r => r.GetProperty("type").GetString() == "Energy");
        energy.GetProperty("energy").GetDouble().ShouldBe(100);
        energy.GetProperty("location").GetString().ShouldBe(_house.Garage);
        var motion = readings.Single(r => r.GetProperty("type").GetString() == "Motion");
        motion.GetProperty("motionDetected").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Readings_ArePagedWithCursor()
    {
        var query = $"/api/v1/readings?location={_house.Kitchen}&location={_house.Garage}&first=5";

        using var firstPage = await GetJsonAsync(query);
        firstPage.RootElement.GetProperty("totalCount").GetInt64().ShouldBe(9);
        firstPage.RootElement.GetProperty("items").GetArrayLength().ShouldBe(5);
        firstPage.RootElement.GetProperty("hasNextPage").GetBoolean().ShouldBeTrue();
        var cursor = firstPage.RootElement.GetProperty("endCursor").GetString();

        using var secondPage = await GetJsonAsync($"{query}&after={cursor}");
        secondPage.RootElement.GetProperty("items").GetArrayLength().ShouldBe(4);
        secondPage.RootElement.GetProperty("hasNextPage").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Aggregates_AcceptCaseInsensitiveEnums()
    {
        using var json = await GetJsonAsync($"/api/v1/aggregates?metric=energy&groupBy=location&location={_house.Kitchen}");

        var kitchen = json.RootElement.EnumerateArray().ShouldHaveSingleItem();
        kitchen.GetProperty("average").GetDouble().ShouldBe(20);
        kitchen.GetProperty("count").GetInt64().ShouldBe(3);
    }

    [Theory]
    [InlineData("/api/v1/readings?first=0", "First")]
    [InlineData("/api/v1/readings?after=not-a-cursor", "After")]
    [InlineData("/api/v1/readings/latest?type=temperature", "type")]
    [InlineData("/api/v1/aggregates?groupBy=time&metric=co2", "Bucket")]
    [InlineData("/api/v1/aggregates?groupBy=location&metric=1", "metric")]
    [InlineData("/api/v1/readings?from=2025-03-02T00:00:00Z&to=2025-03-01T00:00:00Z", "Filter.From")]
    public async Task InvalidInput_Returns400WithFieldErrors(string url, string field)
    {
        using var response = await _http.GetAsync(new Uri(url, UriKind.Relative), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("errors").TryGetProperty(field, out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    [InlineData("/health/ready")]
    public async Task DocumentationAndHealthEndpoints_AreAvailable(string url)
    {
        using var response = await _http.GetAsync(new Uri(url, UriKind.Relative), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Queries_AreExportedAsPrometheusMetrics()
    {
        using var readings = await GetJsonAsync($"/api/v1/readings/latest?location={_house.Garage}");
        using var invalid = await _http.GetAsync(new Uri("/api/v1/readings?first=0", UriKind.Relative), CancellationToken);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var metrics = await _http.GetStringAsync(new Uri("/metrics", UriKind.Relative), CancellationToken);

        const string QueryDuration = "telemetry_gateway_query_duration_milliseconds_count";
        PrometheusText.HasSample(metrics, QueryDuration, ("query", "GetLatestReadingsQuery"), ("outcome", "success")).ShouldBeTrue();
        PrometheusText.HasSample(metrics, QueryDuration, ("query", "GetReadingsQuery"), ("outcome", "invalid")).ShouldBeTrue();
        PrometheusText.HasSample(
            metrics,
            "http_server_request_duration_seconds_count",
            ("http_route", "/api/v1/readings/latest"),
            ("http_response_status_code", "200")).ShouldBeTrue();
    }

    private async Task<JsonDocument> GetJsonAsync(string url)
    {
        using var response = await _http.GetAsync(new Uri(url, UriKind.Relative), CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
    }
}
