using System.ComponentModel.DataAnnotations;

namespace Telemetry.Ingestor.WeakApi;

public sealed class WeakApiOptions : IValidatableObject
{
    public const string SectionName = "WeakApi";

    [Required]
    public Uri? BaseUrl { get; set; }

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the timeout of a single HTTP attempt (the API sometimes never answers).</summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>Gets or sets the timeout of one fetch including all retries.</summary>
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(15);

    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 2;

    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    [Required]
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AttemptTimeout <= TimeSpan.Zero || TotalTimeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "Timeouts must be positive.",
                [nameof(AttemptTimeout), nameof(TotalTimeout)]);
        }

        if (AttemptTimeout > TotalTimeout)
        {
            yield return new ValidationResult(
                $"{nameof(AttemptTimeout)} must not exceed {nameof(TotalTimeout)}.",
                [nameof(AttemptTimeout), nameof(TotalTimeout)]);
        }
    }

    public sealed class CircuitBreakerOptions
    {
        /// <summary>Gets or sets the share of failed attempts within the sampling window that opens the circuit.</summary>
        [Range(0.01, 1.0)]
        public double FailureRatio { get; set; } = 0.8;

        [Range(2, 1000)]
        public int MinimumThroughput { get; set; } = 10;

        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);

        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(20);
    }
}
