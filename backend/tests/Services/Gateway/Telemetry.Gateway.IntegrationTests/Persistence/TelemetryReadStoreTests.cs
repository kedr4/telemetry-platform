using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telemetry.Gateway.Application.Abstractions;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Application.Readings;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Meters;
using Telemetry.Gateway.Domain.Metrics;
using Telemetry.Gateway.Domain.Readings;
using Telemetry.Gateway.Infrastructure;
using Telemetry.Gateway.IntegrationTests.Infrastructure;

namespace Telemetry.Gateway.IntegrationTests.Persistence;

/// <summary>The read store against PostgreSQL with the Processor's schema and a known data set.</summary>
public sealed class TelemetryReadStoreTests(GatewayDatabaseFixture database) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private AsyncServiceScope _scope;
    private SeededHouse _house = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private ITelemetryReadStore Store => _scope.ServiceProvider.GetRequiredService<ITelemetryReadStore>();

    public async ValueTask InitializeAsync()
    {
        _house = await SeededHouse.SeedAsync(database.ConnectionString, CancellationToken);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Telemetry"] = database.ConnectionString })
            .Build();
        _provider = new ServiceCollection().AddLogging().AddGatewayInfrastructure(configuration).BuildServiceProvider();
        _scope = _provider.CreateAsyncScope();
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task GetMeters_FiltersByType()
    {
        var meters = await Store.GetMetersAsync([MeterType.Motion], CancellationToken);

        meters.ShouldContain(meter => meter.Location == _house.Garage && meter.Type == MeterType.Motion);
        meters.ShouldAllBe(meter => meter.Type == MeterType.Motion);
    }

    [Fact]
    public async Task GetLocations_IncludesSeededLocations()
    {
        var locations = await Store.GetLocationsAsync(CancellationToken);

        locations.ShouldContain(_house.Kitchen);
        locations.ShouldContain(_house.Garage);
        locations.ShouldBe(locations.Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    public async Task GetLatestReadings_ReturnsNewestReadingOfEachMeter()
    {
        var latest = await Store.GetLatestReadingsAsync(HouseFilter(), CancellationToken);

        latest.Count.ShouldBe(4);
        latest.OfType<EnergyReading>().Single(r => r.Location == _house.Kitchen).Energy.ShouldBe(30);
        latest.OfType<AirQualityReading>().Single().Co2.ShouldBe(600);
        latest.OfType<EnergyReading>().Single(r => r.Location == _house.Garage).Energy.ShouldBe(100);
        latest.OfType<MotionReading>().Single().RecordedAt.ShouldBe(SeededHouse.At(10, 20));
    }

    [Fact]
    public async Task GetLatestReadings_RespectsTimeRange()
    {
        var latest = await Store.GetLatestReadingsAsync(
            HouseFilter(types: [MeterType.Energy], to: SeededHouse.At(10, 15)),
            CancellationToken);

        latest.OfType<EnergyReading>().Single(r => r.Location == _house.Kitchen).Energy.ShouldBe(20);
    }

    [Fact]
    public async Task GetReadings_PagesThroughAllReadings_NewestFirst_WithoutGapsOrDuplicates()
    {
        var filter = HouseFilter();
        var collected = new List<Reading>();
        ReadingCursor? after = null;

        while (true)
        {
            var slice = await Store.GetReadingsAsync(filter, after, pageSize: 4, CancellationToken);
            slice.TotalCount.ShouldBe(9);
            collected.AddRange(slice.Items);
            if (!slice.HasMore)
            {
                break;
            }

            after = new ReadingCursor(slice.Items[^1].RecordedAt, slice.Items[^1].Id);
        }

        collected.Count.ShouldBe(9);
        collected.Select(r => r.Id).Distinct().Count().ShouldBe(9);
        collected.Select(r => r.RecordedAt).ShouldBeInOrder(SortDirection.Descending);
    }

    [Fact]
    public async Task GetReadings_FiltersByTypeAndTime()
    {
        var slice = await Store.GetReadingsAsync(
            HouseFilter(types: [MeterType.Energy], from: SeededHouse.At(10, 5), to: SeededHouse.At(11, 0)),
            after: null,
            pageSize: 10,
            CancellationToken);

        slice.TotalCount.ShouldBe(2);
        slice.Items.OfType<EnergyReading>().Select(r => r.Energy).ShouldBe([20, 100], ignoreOrder: true);
    }

    [Fact]
    public async Task GetAggregates_MetricByLocation_ComputesStatistics()
    {
        var results = await Store.GetAggregatesAsync(
            new AggregationSpec([AggregationDimension.Location], Metric.Energy, null, HouseFilter()),
            CancellationToken);

        var kitchen = results.Single(r => r.Location == _house.Kitchen);
        (kitchen.Count, kitchen.Average, kitchen.Minimum, kitchen.Maximum, kitchen.Sum).ShouldBe((3L, 20.0, 10.0, 30.0, 60.0));
        results.Single(r => r.Location == _house.Garage).Average.ShouldBe(100);
    }

    [Fact]
    public async Task GetAggregates_MetricByHourAndLocation_BucketsReadings()
    {
        var results = await Store.GetAggregatesAsync(
            new AggregationSpec([AggregationDimension.Time, AggregationDimension.Location], Metric.Energy, TimeBucket.Hour, HouseFilter()),
            CancellationToken);

        var kitchen = results.Where(r => r.Location == _house.Kitchen).ToList();
        kitchen.Select(r => (r.BucketStart, r.Count, r.Average)).ShouldBe(
        [
            (SeededHouse.At(10, 0), 2L, 15.0),
            (SeededHouse.At(11, 0), 1L, 30.0),
        ]);
    }

    [Fact]
    public async Task GetAggregates_WithoutMetric_CountsReadingsByType()
    {
        var results = await Store.GetAggregatesAsync(
            new AggregationSpec([AggregationDimension.Type], null, null, HouseFilter()),
            CancellationToken);

        results.Select(r => (r.Type, r.Count)).ShouldBe(
            [(MeterType.AirQuality, 2L), (MeterType.Energy, 4L), (MeterType.Motion, 3L)],
            ignoreOrder: true);
        results.ShouldAllBe(r => r.Average == null);
    }

    [Fact]
    public async Task GetAggregates_Motion_AverageIsShareOfReadingsWithMotion()
    {
        var results = await Store.GetAggregatesAsync(
            new AggregationSpec([AggregationDimension.Location], Metric.Motion, null, HouseFilter()),
            CancellationToken);

        var garage = results.ShouldHaveSingleItem();
        garage.Location.ShouldBe(_house.Garage);
        garage.Average!.Value.ShouldBe(2.0 / 3, tolerance: 1e-9);
        garage.Sum.ShouldBe(2);
    }

    private ReadingFilter HouseFilter(
        IReadOnlyCollection<MeterType>? types = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null) =>
        new(_house.Locations, types ?? [], from, to);
}
