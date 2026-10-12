using System.ComponentModel.DataAnnotations;

namespace Telemetry.Ingestor.Ingestion;

public sealed class IngestionOptions : IValidatableObject
{
    public const string SectionName = "Ingestion";

    /// <summary>Gets or sets the delay between two ingestion cycles.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>Gets or sets the upper bound for a back-off requested by the API (Retry-After).</summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets after how long without a successful cycle the service reports itself degraded.</summary>
    public TimeSpan StaleAfter { get; set; } = TimeSpan.FromMinutes(2);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PollInterval <= TimeSpan.Zero)
        {
            yield return new ValidationResult("PollInterval must be positive.", [nameof(PollInterval)]);
        }

        if (MaxBackoff < PollInterval)
        {
            yield return new ValidationResult("MaxBackoff must be at least PollInterval.", [nameof(MaxBackoff)]);
        }

        if (StaleAfter <= PollInterval)
        {
            yield return new ValidationResult("StaleAfter must be longer than PollInterval.", [nameof(StaleAfter)]);
        }
    }
}
