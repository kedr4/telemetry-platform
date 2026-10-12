using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using RabbitMQ.Client;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;
using Telemetry.Ingestor.Ingestion;
using Telemetry.Ingestor.IntegrationTests.Infrastructure;
using Telemetry.Testing;
using Telemetry.Testing.RabbitMq;

namespace Telemetry.Ingestor.IntegrationTests;

/// <summary>
/// Runs the real Ingestor application against a fake WeakApp and a real RabbitMQ broker.
/// </summary>
public sealed class IngestorApplicationTests(RabbitMqFixture rabbit) : IDisposable
{
    private static readonly JsonSerializerOptions StatusJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FakeWeakApp _api = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task FlakyApi_ValidReadingsArePublishedToTheBroker()
    {
        _api.Sequence(FakeWeakApp.Status(502, "Bad Gateway"), FakeWeakApp.Ok("""{"error": "data corrupted"}"""), FakeWeakApp.Ok());
        var queue = await DeclareTestQueueAsync();

        await using var factory = CreateFactory();
        using var http = factory.CreateClient();

        var batch = await ReceiveBatchAsync(queue);
        batch.Readings.ShouldBe(
        [
            new EnergyReading("Kitchen", 604.88),
            new AirQualityReading("Garage", 543, 26, 68),
            new MotionReading("Office", MotionDetected: true),
        ]);

        var status = await http.GetFromJsonAsync<IngestionStatusSnapshot>(IngestorEndpoints.StatusPath, StatusJsonOptions, CancellationToken);
        status.ShouldNotBeNull();
        status.PublishedBatches.ShouldBeGreaterThanOrEqualTo(1);
        status.RejectedReadings.ShouldBeGreaterThanOrEqualTo(1);

        var readiness = await http.GetAsync(new Uri("/health/ready", UriKind.Relative), CancellationToken);
        readiness.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("WeakApi:BaseUrl", _api.BaseUrl.ToString())
            .UseSetting("WeakApi:ApiKey", FakeWeakApp.ApiKey)
            .UseSetting("WeakApi:RetryBaseDelay", "00:00:00.010")
            .UseSetting("Ingestion:PollInterval", "00:00:00.200")
            .UseSetting("RabbitMq:HostName", rabbit.HostName)
            .UseSetting("RabbitMq:Port", rabbit.Port.ToString(CultureInfo.InvariantCulture))
            .UseSetting("RabbitMq:UserName", RabbitMqFixture.UserName)
            .UseSetting("RabbitMq:Password", RabbitMqFixture.Password));

    // Stands in for the Processor: a queue bound to the batch routing key, so publishes are routable.
    private async Task<string> DeclareTestQueueAsync()
    {
        var queue = $"tests.ingestor.{Guid.NewGuid():N}";
        await using var connection = await rabbit.CreateConnectionAsync(CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);
        await MessagingTopology.DeclareConsumerQueueAsync(channel, queue, MeterBatchFetched.RoutingKey, CancellationToken);
        return queue;
    }

    private async Task<MeterBatchFetched> ReceiveBatchAsync(string queue)
    {
        await using var connection = await rabbit.CreateConnectionAsync(CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);

        BasicGetResult? delivery = null;
        await Eventually.TrueAsync(
            async () =>
            {
                delivery = await channel.BasicGetAsync(queue, autoAck: true, CancellationToken);
                return delivery is not null;
            },
            CancellationToken);

        return MessageSerializer.Deserialize<MeterBatchFetched>(delivery!.Body);
    }
}
