using Telemetry.Contracts.Readings;

namespace Telemetry.Processor.Domain;

/// <summary>A physical meter, identified by its location and type.</summary>
public sealed class Meter
{
    public const int MaxLocationLength = 64;

    private Meter(string location, MeterType type)
    {
        Location = location;
        Type = type;
    }

    // Get-only: assigned by the database and written by EF Core through the backing field.
    public int Id { get; }

    public string Location { get; private set; }

    public MeterType Type { get; private set; }

    public static Meter Create(string location, MeterType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        if (location.Length > MaxLocationLength || location != location.Trim())
        {
            throw new ArgumentException(
                $"Location must be trimmed and at most {MaxLocationLength} characters long.",
                nameof(location));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown meter type.");
        }

        return new Meter(location, type);
    }
}
