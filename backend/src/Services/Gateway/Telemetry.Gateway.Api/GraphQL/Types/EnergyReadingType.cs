using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Api.GraphQL.Types;

internal sealed class EnergyReadingType : ObjectType<EnergyReading>
{
    protected override void Configure(IObjectTypeDescriptor<EnergyReading> descriptor)
    {
        descriptor.Implements<ReadingInterfaceType>();
        descriptor.Description("Energy consumption reading.");
        descriptor.Field(reading => reading.Energy).Description("Consumed energy (kWh).");
    }
}
