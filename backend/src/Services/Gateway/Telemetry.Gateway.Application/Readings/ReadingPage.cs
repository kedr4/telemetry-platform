using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Application.Readings;

/// <summary>One page of readings, newest first.</summary>
/// <param name="Items">Readings on this page.</param>
/// <param name="TotalCount">Number of readings matching the filter across all pages.</param>
/// <param name="HasNextPage">Whether more readings follow.</param>
/// <param name="EndCursor">Cursor to request the next page with, if there is one.</param>
public sealed record ReadingPage(IReadOnlyList<Reading> Items, long TotalCount, bool HasNextPage, string? EndCursor);
