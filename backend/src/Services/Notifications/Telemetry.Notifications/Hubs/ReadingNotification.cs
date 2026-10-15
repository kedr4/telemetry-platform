using Telemetry.Contracts.Readings;

namespace Telemetry.Notifications.Hubs;

/// <summary>
/// A stored reading as pushed to clients. Flat, with the values of its meter type set
/// (same names as the REST and GraphQL APIs); the others are omitted from the JSON.
/// </summary>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="Type">Kind of meter.</param>
/// <param name="RecordedAt">When the reading was taken.</param>
/// <param name="Energy">Consumed energy (kWh), for energy meters.</param>
/// <param name="Co2">CO2 concentration (ppm), for air quality meters.</param>
/// <param name="Pm25">PM2.5 concentration (µg/m³), for air quality meters.</param>
/// <param name="Humidity">Relative humidity (%), for air quality meters.</param>
/// <param name="MotionDetected">Whether motion was detected, for motion meters.</param>
public sealed record ReadingNotification(
    string Location,
    MeterType Type,
    DateTimeOffset RecordedAt,
    double? Energy = null,
    int? Co2 = null,
    int? Pm25 = null,
    int? Humidity = null,
    bool? MotionDetected = null)
{
    public static ReadingNotification From(MeterReading reading, DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(reading);

        return reading switch
        {
            EnergyReading energy => new(energy.Location, MeterType.Energy, recordedAt, Energy: energy.Energy),
            AirQualityReading air => new(
                air.Location, MeterType.AirQuality, recordedAt, Co2: air.Co2, Pm25: air.Pm25, Humidity: air.Humidity),
            MotionReading motion => new(motion.Location, MeterType.Motion, recordedAt, MotionDetected: motion.MotionDetected),
            _ => throw new ArgumentException($"Unsupported reading type {reading.GetType().Name}.", nameof(reading)),
        };
    }
}
