namespace Telemetry.Gateway.Infrastructure.Persistence;

/// <summary>Row of the <c>meters</c> table (schema owned by the Processor).</summary>
internal sealed class MeterRow
{
    public int Id { get; set; }

    public string Location { get; set; } = string.Empty;

    /// <summary>Gets or sets the wire name of the meter type, e.g. <c>air_quality</c>.</summary>
    public string Type { get; set; } = string.Empty;
}
