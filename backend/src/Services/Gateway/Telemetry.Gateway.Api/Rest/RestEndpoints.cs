using System.ComponentModel;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Telemetry.Gateway.Application.Aggregations.GetAggregates;
using Telemetry.Gateway.Application.Meters.GetLocations;
using Telemetry.Gateway.Application.Meters.GetMeters;
using Telemetry.Gateway.Application.Readings;
using Telemetry.Gateway.Application.Readings.GetLatestReadings;
using Telemetry.Gateway.Application.Readings.GetReadings;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Meters;
using Telemetry.Gateway.Domain.Metrics;
using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Api.Rest;

/// <summary>
/// REST endpoints. They send the same MediatR queries as the GraphQL resolvers, so both APIs share
/// validation and data access.
/// </summary>
internal static class RestEndpoints
{
    public const string BasePath = "/api/v1";

    public static IEndpointRouteBuilder MapRestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup(BasePath).ProducesValidationProblem();

        api.MapGet("/meters", GetMetersAsync)
            .WithName("GetMeters")
            .WithTags("Meters")
            .WithSummary("All meters, optionally of some types.");

        api.MapGet("/locations", GetLocationsAsync)
            .WithName("GetLocations")
            .WithTags("Meters")
            .WithSummary("All locations that have meters.");

        api.MapGet("/readings/latest", GetLatestReadingsAsync)
            .WithName("GetLatestReadings")
            .WithTags("Readings")
            .WithSummary("The most recent reading of every meter matching the filter.");

        api.MapGet("/readings", GetReadingsAsync)
            .WithName("GetReadings")
            .WithTags("Readings")
            .WithSummary("Readings matching the filter, newest first, with cursor pagination.");

        api.MapGet("/aggregates", GetAggregatesAsync)
            .WithName("GetAggregates")
            .WithTags("Aggregations")
            .WithSummary("Statistics of a metric, or reading counts, grouped by location, type and/or time.");

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<Meter>>> GetMetersAsync(
        [FromQuery(Name = "type")][Description("Meter types to include (repeat for several).")] string[]? types,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetMetersQuery(QueryValues.ParseEnums<MeterType>(types, "type")), cancellationToken));

    private static async Task<Ok<IReadOnlyList<string>>> GetLocationsAsync(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetLocationsQuery(), cancellationToken));

    private static async Task<Ok<IReadOnlyList<Reading>>> GetLatestReadingsAsync(
        [AsParameters] ReadingFilterParameters filter,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetLatestReadingsQuery(filter.ToFilter()), cancellationToken));

    private static async Task<Ok<ReadingPage>> GetReadingsAsync(
        [AsParameters] ReadingFilterParameters filter,
        [Description("Page size (1-500).")] int? first,
        [Description("Cursor of the previous page (its endCursor).")] string? after,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetReadingsQuery(filter.ToFilter(), first ?? GetReadingsQuery.DefaultPageSize, after),
            cancellationToken));

    private static async Task<Ok<IReadOnlyList<AggregateResult>>> GetAggregatesAsync(
        [AsParameters] ReadingFilterParameters filter,
        [FromQuery(Name = "groupBy")][Description("Dimensions: Location, Type, Time (repeat to combine).")] string[]? groupBy,
        [Description("Metric: Energy, Co2, Pm25, Humidity, Motion. Omit to count readings.")] string? metric,
        [Description("Time bucket (Minute, Hour, Day); required when grouping by Time.")] string? bucket,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetAggregatesQuery(
                QueryValues.ParseEnums<AggregationDimension>(groupBy, "groupBy"),
                QueryValues.ParseOptionalEnum<Metric>(metric, "metric"),
                QueryValues.ParseOptionalEnum<TimeBucket>(bucket, "bucket"),
                filter.ToFilter()),
            cancellationToken));
}
