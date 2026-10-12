using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Telemetry.Contracts.Readings;
using Telemetry.Ingestor.WeakApi;

namespace Telemetry.Ingestor.Ingestion;

/// <summary>
/// Translates an untrusted upstream meter into a strongly typed reading. Checks structure only
/// (known type, required members of the right JSON kind); value ranges are validated separately.
/// </summary>
internal static class ReadingMapper
{
    public static bool TryMap(
        WeakApiMeterDto meter,
        [NotNullWhen(true)] out MeterReading? reading,
        [NotNullWhen(false)] out string? error)
    {
        reading = null;

        if (string.IsNullOrWhiteSpace(meter.Name))
        {
            error = "Meter has no name.";
            return false;
        }

        if (!MeterTypeNames.TryParse(meter.Type, out var type))
        {
            error = $"Unknown meter type '{meter.Type}'.";
            return false;
        }

        if (meter.Payload.ValueKind != JsonValueKind.Object)
        {
            error = "Meter has no payload object.";
            return false;
        }

        var location = meter.Name.Trim();
        var payload = meter.Payload;
        reading = type switch
        {
            MeterType.Energy => TryGetDouble(payload, "energy", out var energy)
                ? new EnergyReading(location, energy)
                : null,
            MeterType.AirQuality => TryGetInt(payload, "co2", out var co2)
                && TryGetInt(payload, "pm25", out var pm25)
                && TryGetInt(payload, "humidity", out var humidity)
                    ? new AirQualityReading(location, co2, pm25, humidity)
                    : null,
            MeterType.Motion => TryGetBool(payload, "motionDetected", out var motion)
                ? new MotionReading(location, motion)
                : null,
            _ => null,
        };

        error = reading is null ? $"Payload of '{meter.Type}' meter '{location}' is incomplete or malformed." : null;
        return reading is not null;
    }

    private static bool TryGetDouble(JsonElement payload, string name, out double value)
    {
        value = default;
        return payload.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out value);
    }

    private static bool TryGetInt(JsonElement payload, string name, out int value)
    {
        value = default;
        return payload.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out value);
    }

    private static bool TryGetBool(JsonElement payload, string name, out bool value)
    {
        value = default;
        if (!payload.TryGetProperty(name, out var property)
            || property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = property.GetBoolean();
        return true;
    }
}
