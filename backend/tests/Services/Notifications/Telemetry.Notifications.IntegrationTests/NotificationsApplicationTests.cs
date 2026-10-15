using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;
using Telemetry.Notifications.Hubs;
using Telemetry.Testing;
using Telemetry.Testing.RabbitMq;

namespace Telemetry.Notifications.IntegrationTests;

/// <summary>
/// Runs the real service against RabbitMQ and connects real SignalR clients to it.
/// </summary>
public sealed class NotificationsApplicationTests(RabbitMqFixture rabbit) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("RabbitMq:HostName", rabbit.HostName)
            .UseSetting("RabbitMq:Port", rabbit.Port.ToString(CultureInfo.InvariantCulture))
            .UseSetting("RabbitMq:UserName", RabbitMqFixture.UserName)
            .UseSetting("RabbitMq:Password", RabbitMqFixture.Password));
        _factory.CreateClient().Dispose(); // start the server

        await rabbit.WaitForConsumerAsync(QueueNames.NotificationsReadingsPersisted, CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task EveryClient_ReceivesStoredBatches()
    {
        var received = new ConcurrentQueue<ReadingsUpdatedNotification>();
        await using var client = await ConnectAsync(hub => hub.On<ReadingsUpdatedNotification>(nameof(ITelemetryClient.ReadingsUpdated), received.Enqueue));
        var location = UniqueLocation();

        var message = await PublishAsync(new EnergyReading(location, 604.88), new AirQualityReading(location, 543, 26, 68));

        await Eventually.TrueAsync(() => received.Any(n => n.BatchId == message.BatchId), CancellationToken);
        var notification = received.Single(n => n.BatchId == message.BatchId);
        notification.Readings.ShouldBe(
        [
            new ReadingNotification(location, MeterType.Energy, message.RecordedAt, Energy: 604.88),
            new ReadingNotification(location, MeterType.AirQuality, message.RecordedAt, Co2: 543, Pm25: 26, Humidity: 68),
        ]);
    }

    [Fact]
    public async Task SubscribedClient_ReceivesOnlyItsLocation_UntilItUnsubscribes()
    {
        var (kitchen, garage) = (UniqueLocation(), UniqueLocation());
        var broadcasts = new ConcurrentQueue<ReadingsUpdatedNotification>();
        var locationUpdates = new ConcurrentQueue<(string Location, ReadingsUpdatedNotification Notification)>();
        await using var client = await ConnectAsync(hub =>
        {
            hub.On<ReadingsUpdatedNotification>(nameof(ITelemetryClient.ReadingsUpdated), broadcasts.Enqueue);
            hub.On<string, ReadingsUpdatedNotification>(
                nameof(ITelemetryClient.LocationReadingsUpdated),
                (location, notification) => locationUpdates.Enqueue((location, notification)));
        });
        await client.InvokeAsync(nameof(TelemetryHub.SubscribeToLocation), kitchen, CancellationToken);

        var subscribed = await PublishAsync(new EnergyReading(kitchen, 1), new EnergyReading(garage, 2));
        await Eventually.TrueAsync(() => locationUpdates.Any(u => u.Notification.BatchId == subscribed.BatchId), CancellationToken);

        var (location, notification) = locationUpdates.Single(u => u.Notification.BatchId == subscribed.BatchId);
        location.ShouldBe(kitchen);
        notification.Readings.ShouldHaveSingleItem().Location.ShouldBe(kitchen);

        await client.InvokeAsync(nameof(TelemetryHub.UnsubscribeFromLocation), kitchen, CancellationToken);
        var unsubscribed = await PublishAsync(new EnergyReading(kitchen, 3));
        var marker = await PublishAsync(new EnergyReading(garage, 4));

        // Messages on a connection keep their order, and each batch's broadcast precedes its location
        // messages: once the later marker's broadcast arrived, a location message for the earlier batch
        // would have arrived too.
        await Eventually.TrueAsync(() => broadcasts.Any(b => b.BatchId == marker.BatchId), CancellationToken);
        locationUpdates.ShouldNotContain(u => u.Notification.BatchId == unsubscribed.BatchId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a-location-name-that-is-much-longer-than-sixty-four-characters-allowed")]
    public async Task SubscribingToInvalidLocation_IsRejected(string location)
    {
        await using var client = await ConnectAsync(_ => { });

        var exception = await Should.ThrowAsync<HubException>(() =>
            client.InvokeAsync(nameof(TelemetryHub.SubscribeToLocation), location, CancellationToken));

        exception.Message.ShouldContain("Location must be");
    }

    [Fact]
    public async Task Readiness_IsHealthy()
    {
        using var http = _factory.CreateClient();

        using var response = await http.GetAsync(new Uri("/health/ready", UriKind.Relative), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static string UniqueLocation() => $"Room-{Guid.NewGuid():N}"[..13];

    private async Task<HubConnection> ConnectAsync(Action<HubConnection> registerHandlers)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, TelemetryHub.Path), options =>
            {
                // The in-memory test server supports long polling only.
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        registerHandlers(connection);
        await connection.StartAsync(CancellationToken);
        return connection;
    }

    private async Task<ReadingsPersisted> PublishAsync(params MeterReading[] readings)
    {
        var message = new ReadingsPersisted(Guid.CreateVersion7(), DateTimeOffset.UtcNow, readings);
        await using var connection = await rabbit.CreateConnectionAsync(CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);
        await channel.BasicPublishAsync(
            MessagingTopology.Exchange,
            ReadingsPersisted.RoutingKey,
            MessageSerializer.Serialize(message),
            CancellationToken);
        return message;
    }
}
