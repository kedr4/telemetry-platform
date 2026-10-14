using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Api.GraphQL.Inputs;

/// <summary>Restricts readings; omitted fields do not restrict.</summary>
/// <param name="Locations">Locations to include.</param>
/// <param name="Types">Meter types to include.</param>
/// <param name="From">Inclusive lower bound of the reading time.</param>
/// <param name="To">Exclusive upper bound of the reading time.</param>
[GraphQLDescription("Restricts readings; omitted fields do not restrict.")]
public sealed record ReadingFilterInput(
    [property: GraphQLDescription("Locations to include.")] IReadOnlyList<string>? Locations = null,
    [property: GraphQLDescription("Meter types to include.")] IReadOnlyList<MeterType>? Types = null,
    [property: GraphQLDescription("Inclusive lower bound of the reading time.")] DateTimeOffset? From = null,
    [property: GraphQLDescription("Exclusive upper bound of the reading time.")] DateTimeOffset? To = null);
