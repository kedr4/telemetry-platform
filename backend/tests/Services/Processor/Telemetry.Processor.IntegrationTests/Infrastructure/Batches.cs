using Telemetry.Contracts.Messaging;
using Telemetry.Contracts.Readings;

namespace Telemetry.Processor.IntegrationTests.Infrastructure;

/// <summary>Builds batches whose locations are unique to the calling test.</summary>
internal static class Batches
{
    public static string UniqueLocation(string room) => $"{room}-{Guid.NewGuid():N}"[..Math.Min(room.Length + 9, 64)];

    public static MeterBatchFetched Create(params MeterReading[] readings) =>
        new(Guid.CreateVersion7(), DateTimeOffset.UtcNow, readings);

    public static MeterBatchFetched ForLocation(string location) => Create(
        new EnergyReading(location, 604.88),
        new AirQualityReading(location, 543, 26, 68),
        new MotionReading(location, MotionDetected: true));
}
