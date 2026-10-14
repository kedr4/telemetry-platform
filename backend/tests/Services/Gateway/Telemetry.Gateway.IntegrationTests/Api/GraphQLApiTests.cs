using System.Net.Http.Json;
using System.Text.Json;
using Telemetry.Gateway.IntegrationTests.Infrastructure;

namespace Telemetry.Gateway.IntegrationTests.Api;

public sealed class GraphQLApiTests(GatewayDatabaseFixture database) : IAsyncLifetime
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
    public async Task LatestReadings_ResolveConcreteTypesThroughTheInterface()
    {
        using var result = await QueryAsync(
            """
            query Latest($locations: [String!]) {
              latestReadings(filter: { locations: $locations }) {
                __typename
                location
                ... on EnergyReading { energy }
                ... on AirQualityReading { co2 pm25 humidity }
                ... on MotionReading { motionDetected }
              }
            }
            """,
            new { locations = new[] { _house.Kitchen } });

        var readings = Data(result).GetProperty("latestReadings").EnumerateArray().ToList();
        readings.Select(r => r.GetProperty("__typename").GetString()).ShouldBe(["AirQualityReading", "EnergyReading"], ignoreOrder: true);
        readings.Single(r => r.GetProperty("__typename").GetString() == "AirQualityReading").GetProperty("co2").GetInt32().ShouldBe(600);
    }

    [Fact]
    public async Task Readings_ReturnConnectionWithPageInfo()
    {
        using var result = await QueryAsync(
            """
            query Page($locations: [String!]) {
              readings(first: 2, filter: { locations: $locations, types: [MOTION] }) {
                totalCount
                pageInfo { hasNextPage endCursor }
                nodes { id recordedAt }
              }
            }
            """,
            new { locations = new[] { _house.Garage } });

        var readings = Data(result).GetProperty("readings");
        readings.GetProperty("totalCount").GetInt64().ShouldBe(3);
        readings.GetProperty("nodes").GetArrayLength().ShouldBe(2);
        readings.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean().ShouldBeTrue();
        readings.GetProperty("pageInfo").GetProperty("endCursor").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Aggregates_GroupByTimeAndLocation()
    {
        using var result = await QueryAsync(
            """
            query Series($locations: [String!]) {
              aggregates(input: { groupBy: [TIME, LOCATION], metric: ENERGY, bucket: HOUR, filter: { locations: $locations } }) {
                location bucketStart count average
              }
            }
            """,
            new { locations = new[] { _house.Kitchen } });

        var groups = Data(result).GetProperty("aggregates").EnumerateArray()
            .Select(g => (g.GetProperty("count").GetInt64(), g.GetProperty("average").GetDouble()))
            .ToList();
        groups.ShouldBe([(2L, 15.0), (1L, 30.0)]);
    }

    [Fact]
    public async Task InvalidAggregation_ReturnsValidationError()
    {
        using var result = await QueryAsync("{ aggregates(input: { groupBy: [TIME], metric: CO2 }) { count } }");

        var error = result.RootElement.GetProperty("errors").EnumerateArray().ShouldHaveSingleItem();
        var extensions = error.GetProperty("extensions");
        extensions.GetProperty("code").GetString().ShouldBe("VALIDATION_ERROR");
        extensions.GetProperty("errors").EnumerateArray()
            .ShouldContain(e => e.GetProperty("field").GetString() == "Bucket");
    }

    [Fact]
    public async Task UnknownEnumValue_IsRejectedByTheSchema()
    {
        using var result = await QueryAsync("{ meters(types: [TEMPERATURE]) { id } }");

        result.RootElement.TryGetProperty("errors", out var errors).ShouldBeTrue();
        errors.GetArrayLength().ShouldBeGreaterThan(0);
    }

    private static JsonElement Data(JsonDocument result)
    {
        result.RootElement.TryGetProperty("errors", out var errors).ShouldBeFalse(errors.ToString());
        return result.RootElement.GetProperty("data");
    }

    private async Task<JsonDocument> QueryAsync(string query, object? variables = null)
    {
        using var response = await _http.PostAsJsonAsync(new Uri("/graphql", UriKind.Relative), new { query, variables }, CancellationToken);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
    }
}
