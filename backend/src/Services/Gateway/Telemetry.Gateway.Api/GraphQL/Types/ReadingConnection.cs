using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Api.GraphQL.Types;

/// <summary>One page of readings.</summary>
/// <param name="Nodes">Readings on this page, newest first.</param>
/// <param name="PageInfo">Where this page ends.</param>
/// <param name="TotalCount">Number of readings matching the filter across all pages.</param>
[GraphQLDescription("One page of readings.")]
public sealed record ReadingConnection(
    [property: GraphQLDescription("Readings on this page, newest first.")] IReadOnlyList<Reading> Nodes,
    [property: GraphQLDescription("Where this page ends.")] ReadingPageInfo PageInfo,
    [property: GraphQLDescription("Number of readings matching the filter across all pages.")] long TotalCount);
