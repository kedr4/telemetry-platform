using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Api.GraphQL.Types;

/// <summary>The <c>Reading</c> interface, implemented by one object type per meter type.</summary>
internal sealed class ReadingInterfaceType : InterfaceType<Reading>
{
    protected override void Configure(IInterfaceTypeDescriptor<Reading> descriptor)
    {
        descriptor.Name("Reading");
        descriptor.Description("A measurement taken by a meter.");
        descriptor.Field(reading => reading.Id).Description("Identifier of the reading.");
        descriptor.Field(reading => reading.MeterId).Description("Identifier of the meter that took the reading.");
        descriptor.Field(reading => reading.Location).Description("Room the meter is installed in.");
        descriptor.Field(reading => reading.Type).Description("Kind of meter.");
        descriptor.Field(reading => reading.RecordedAt).Description("When the reading was taken.");
    }
}
