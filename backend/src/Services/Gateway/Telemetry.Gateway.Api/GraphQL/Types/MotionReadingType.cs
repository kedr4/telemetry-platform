using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Api.GraphQL.Types;

internal sealed class MotionReadingType : ObjectType<MotionReading>
{
    protected override void Configure(IObjectTypeDescriptor<MotionReading> descriptor)
    {
        descriptor.Implements<ReadingInterfaceType>();
        descriptor.Description("Motion sensor reading.");
        descriptor.Field(reading => reading.MotionDetected).Description("Whether motion was detected.");
    }
}
