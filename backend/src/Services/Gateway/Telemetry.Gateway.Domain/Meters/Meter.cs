namespace Telemetry.Gateway.Domain.Meters;

/// <summary>A physical meter, identified by its location and type.</summary>
/// <param name="Id">Database identifier.</param>
/// <param name="Location">Room the meter is installed in.</param>
/// <param name="Type">Kind of meter.</param>
public sealed record Meter(int Id, string Location, MeterType Type);
