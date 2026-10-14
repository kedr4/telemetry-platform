using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Api.GraphQL.Types;

internal sealed class MeterObjectType : ObjectType<Meter>
{
    protected override void Configure(IObjectTypeDescriptor<Meter> descriptor)
    {
        descriptor.Name("Meter");
        descriptor.Description("A physical meter, identified by its location and type.");
        descriptor.Field(meter => meter.Id).Description("Identifier of the meter.");
        descriptor.Field(meter => meter.Location).Description("Room the meter is installed in.");
        descriptor.Field(meter => meter.Type).Description("Kind of meter.");
    }
}
