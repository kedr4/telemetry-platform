using Telemetry.Gateway.Domain.Aggregations;

namespace Telemetry.Gateway.Api.GraphQL.Types;

internal sealed class AggregateResultType : ObjectType<AggregateResult>
{
    protected override void Configure(IObjectTypeDescriptor<AggregateResult> descriptor)
    {
        descriptor.Name("Aggregate");
        descriptor.Description(
            "Statistics of one group. Keys that were not grouped by are null; value statistics are null when no metric was requested.");
        descriptor.Field(result => result.Location).Description("Location of the group (when grouped by LOCATION).");
        descriptor.Field(result => result.Type).Description("Meter type of the group (when grouped by TYPE).");
        descriptor.Field(result => result.BucketStart).Description("Start of the time bucket (when grouped by TIME).");
        descriptor.Field(result => result.Count).Description("Number of readings in the group.");
        descriptor.Field(result => result.Average).Description("Average of the metric.");
        descriptor.Field(result => result.Minimum).Description("Minimum of the metric.");
        descriptor.Field(result => result.Maximum).Description("Maximum of the metric.");
        descriptor.Field(result => result.Sum).Description("Sum of the metric.");
    }
}
