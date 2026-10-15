using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;
using Telemetry.Notifications.Diagnostics;
using Telemetry.Notifications.Hubs;
using Telemetry.Notifications.Messaging;

namespace Telemetry.Notifications.UnitTests.Messaging;

public sealed class ReadingsPersistedHandlerTests : IDisposable
{
    private static readonly ReadingsPersisted Message = new(
        Guid.CreateVersion7(),
        new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero),
        [new EnergyReading("Kitchen", 604.88), new MotionReading("Kitchen", MotionDetected: false), new EnergyReading("Garage", 12.5)]);

    private readonly IHubContext<TelemetryHub, ITelemetryClient> _hub = Substitute.For<IHubContext<TelemetryHub, ITelemetryClient>>();
    private readonly ITelemetryClient _everyone = Substitute.For<ITelemetryClient>();
    private readonly ITelemetryClient _kitchen = Substitute.For<ITelemetryClient>();
    private readonly ITelemetryClient _garage = Substitute.For<ITelemetryClient>();
    private readonly ServiceProvider _metricsProvider = new ServiceCollection().AddMetrics().BuildServiceProvider();
    private readonly ReadingsPersistedHandler _handler;

    public ReadingsPersistedHandlerTests()
    {
        _hub.Clients.All.Returns(_everyone);
        _hub.Clients.Group(TelemetryHub.LocationGroup("Kitchen")).Returns(_kitchen);
        _hub.Clients.Group(TelemetryHub.LocationGroup("Garage")).Returns(_garage);

        _handler = new ReadingsPersistedHandler(
            _hub,
            new NotificationsMetrics(_metricsProvider.GetRequiredService<IMeterFactory>()),
            NullLogger<ReadingsPersistedHandler>.Instance);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => _metricsProvider.Dispose();

    [Fact]
    public async Task Handle_BroadcastsWholeBatchToEveryone()
    {
        await _handler.HandleAsync(Message, new MessageContext("1", "c", Redelivered: false), CancellationToken);

        await _everyone.Received(1).ReadingsUpdated(Arg.Is<ReadingsUpdatedNotification>(n =>
            n.BatchId == Message.BatchId && n.RecordedAt == Message.RecordedAt && n.Readings.Count == 3));
    }

    [Fact]
    public async Task Handle_SendsEachLocationOnlyItsOwnReadings()
    {
        await _handler.HandleAsync(Message, new MessageContext("1", "c", Redelivered: false), CancellationToken);

        await _kitchen.Received(1).LocationReadingsUpdated(
            "Kitchen",
            Arg.Is<ReadingsUpdatedNotification>(n => n.Readings.Count == 2 && n.Readings.All(r => r.Location == "Kitchen")));
        await _garage.Received(1).LocationReadingsUpdated(
            "Garage",
            Arg.Is<ReadingsUpdatedNotification>(n => n.Readings.Count == 1 && n.Readings[0].Location == "Garage"));
    }
}
