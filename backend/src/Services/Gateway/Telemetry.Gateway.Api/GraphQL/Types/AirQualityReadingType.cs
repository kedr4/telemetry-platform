using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Api.GraphQL.Types;

internal sealed class AirQualityReadingType : ObjectType<AirQualityReading>
{
    protected override void Configure(IObjectTypeDescriptor<AirQualityReading> descriptor)
    {
        descriptor.Implements<ReadingInterfaceType>();
        descriptor.Description("Air quality reading.");
        descriptor.Field(reading => reading.Co2).Description("CO2 concentration (ppm).");
        descriptor.Field(reading => reading.Pm25).Description("Fine particulate matter PM2.5 (µg/m³).");
        descriptor.Field(reading => reading.Humidity).Description("Relative humidity (%).");
    }
}
