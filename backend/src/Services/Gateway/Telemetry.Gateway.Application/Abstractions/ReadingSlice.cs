using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Application.Abstractions;

/// <summary>Readings returned by the store for one page request.</summary>
/// <param name="Items">At most the requested number of readings, newest first.</param>
/// <param name="TotalCount">Number of readings matching the filter, ignoring the cursor.</param>
/// <param name="HasMore">Whether more readings follow the returned ones.</param>
public sealed record ReadingSlice(IReadOnlyList<Reading> Items, long TotalCount, bool HasMore);
