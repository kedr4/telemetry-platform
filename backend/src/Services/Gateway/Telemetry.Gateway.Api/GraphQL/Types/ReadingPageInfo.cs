namespace Telemetry.Gateway.Api.GraphQL.Types;

/// <summary>Pagination state of a reading page.</summary>
/// <param name="HasNextPage">Whether more readings follow.</param>
/// <param name="EndCursor">Cursor to pass as <c>after</c> for the next page.</param>
[GraphQLDescription("Pagination state of a reading page.")]
public sealed record ReadingPageInfo(
    [property: GraphQLDescription("Whether more readings follow.")] bool HasNextPage,
    [property: GraphQLDescription("Cursor to pass as `after` for the next page.")] string? EndCursor);
