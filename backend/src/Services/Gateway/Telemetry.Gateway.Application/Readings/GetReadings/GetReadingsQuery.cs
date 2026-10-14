using MediatR;
using Telemetry.Gateway.Application.Common;

namespace Telemetry.Gateway.Application.Readings.GetReadings;

/// <summary>A page of readings matching the filter, newest first (keyset pagination).</summary>
/// <param name="Filter">Readings to include.</param>
/// <param name="First">Page size.</param>
/// <param name="After">Cursor returned with the previous page; <c>null</c> for the first page.</param>
public sealed record GetReadingsQuery(ReadingFilter Filter, int First = GetReadingsQuery.DefaultPageSize, string? After = null)
    : IRequest<ReadingPage>
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 500;
}
