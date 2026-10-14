using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Api.Rest;

/// <summary>Query-string filter shared by the reading and aggregation endpoints.</summary>
internal sealed class ReadingFilterParameters
{
    [FromQuery(Name = "location")]
    [Description("Locations to include (repeat the parameter for several).")]
    public string[]? Locations { get; init; }

    [FromQuery(Name = "type")]
    [Description("Meter types to include: Energy, AirQuality, Motion (repeat for several).")]
    public string[]? Types { get; init; }

    [FromQuery(Name = "from")]
    [Description("Inclusive lower bound of the reading time (ISO 8601).")]
    public DateTimeOffset? From { get; init; }

    [FromQuery(Name = "to")]
    [Description("Exclusive upper bound of the reading time (ISO 8601).")]
    public DateTimeOffset? To { get; init; }

    public ReadingFilter ToFilter() => new(
        Locations ?? [],
        QueryValues.ParseEnums<MeterType>(Types, "type"),
        From,
        To);
}
