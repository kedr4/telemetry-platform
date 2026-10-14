using System.Text.Json.Serialization.Metadata;
using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Api.Json;

/// <summary>
/// Serializes readings polymorphically for REST, keeping the domain free of serialization attributes:
/// <c>{"type": "AirQuality", "id": 1, "location": "Garage", "recordedAt": "…", "co2": 543, …}</c>.
/// The <c>type</c> discriminator replaces the readings' own <c>Type</c> property.
/// </summary>
internal static class ReadingJsonPolymorphism
{
    public static void Apply(JsonTypeInfo typeInfo)
    {
        if (!typeof(Reading).IsAssignableFrom(typeInfo.Type))
        {
            return;
        }

        var typeProperty = typeInfo.Properties.FirstOrDefault(property => property.Name == "type");
        if (typeProperty is not null)
        {
            typeInfo.Properties.Remove(typeProperty);
        }

        if (typeInfo.Type == typeof(Reading))
        {
            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "type",
                DerivedTypes =
                {
                    new JsonDerivedType(typeof(EnergyReading), "Energy"),
                    new JsonDerivedType(typeof(AirQualityReading), "AirQuality"),
                    new JsonDerivedType(typeof(MotionReading), "Motion"),
                },
            };
        }
    }
}
