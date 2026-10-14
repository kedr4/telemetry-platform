using MediatR;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Application.Readings.GetLatestReadings;

/// <summary>The most recent reading of every meter matching the filter.</summary>
/// <param name="Filter">Meters (and time range) to consider.</param>
public sealed record GetLatestReadingsQuery(ReadingFilter Filter) : IRequest<IReadOnlyList<Reading>>;
