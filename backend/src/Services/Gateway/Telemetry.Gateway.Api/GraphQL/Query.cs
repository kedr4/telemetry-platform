using MediatR;
using Telemetry.Gateway.Api.GraphQL.Inputs;
using Telemetry.Gateway.Api.GraphQL.Types;
using Telemetry.Gateway.Application.Aggregations.GetAggregates;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Application.Meters.GetLocations;
using Telemetry.Gateway.Application.Meters.GetMeters;
using Telemetry.Gateway.Application.Readings.GetLatestReadings;
using Telemetry.Gateway.Application.Readings.GetReadings;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Meters;
using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Api.GraphQL;

/// <summary>
/// GraphQL entry points. Resolvers send the same MediatR queries as the REST endpoints.
/// </summary>
[GraphQLDescription("Smart-home telemetry: meters, readings and aggregations.")]
public sealed class Query
{
    [GraphQLDescription("All meters, optionally only of the given types.")]
    public Task<IReadOnlyList<Meter>> GetMetersAsync(
        IReadOnlyList<MeterType>? types,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new GetMetersQuery(types ?? []), cancellationToken);

    [GraphQLDescription("All locations that have meters, alphabetically.")]
    public Task<IReadOnlyList<string>> GetLocationsAsync([Service] ISender sender, CancellationToken cancellationToken) =>
        sender.Send(new GetLocationsQuery(), cancellationToken);

    [GraphQLDescription("The most recent reading of every meter matching the filter.")]
    public Task<IReadOnlyList<Reading>> GetLatestReadingsAsync(
        ReadingFilterInput? filter,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new GetLatestReadingsQuery(ToFilter(filter)), cancellationToken);

    [GraphQLDescription("Readings matching the filter, newest first. Pass `pageInfo.endCursor` as `after` to get the next page.")]
    public async Task<ReadingConnection> GetReadingsAsync(
        [Service] ISender sender,
        ReadingFilterInput? filter,
        [GraphQLDescription("Page size (1-500).")] int first = GetReadingsQuery.DefaultPageSize,
        [GraphQLDescription("Cursor of the previous page.")] string? after = null,
        CancellationToken cancellationToken = default)
    {
        var page = await sender.Send(new GetReadingsQuery(ToFilter(filter), first, after), cancellationToken);
        return new ReadingConnection(page.Items, new ReadingPageInfo(page.HasNextPage, page.EndCursor), page.TotalCount);
    }

    [GraphQLDescription("Statistics of a metric, or reading counts, grouped by location, type and/or time bucket.")]
    public Task<IReadOnlyList<AggregateResult>> GetAggregatesAsync(
        AggregationInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(
            new GetAggregatesQuery(input.GroupBy, input.Metric, input.Bucket, ToFilter(input.Filter)),
            cancellationToken);

    private static ReadingFilter ToFilter(ReadingFilterInput? filter) =>
        filter is null
            ? ReadingFilter.None
            : new ReadingFilter(filter.Locations ?? [], filter.Types ?? [], filter.From, filter.To);
}
