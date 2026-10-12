using FluentValidation;
using Telemetry.Contracts.Readings;

namespace Telemetry.Ingestor.Ingestion.Validation;

/// <summary>
/// Business validation of a reading: plausible location and physically sensible values per meter type.
/// </summary>
internal sealed class MeterReadingValidator : AbstractValidator<MeterReading>
{
    public const int MaxLocationLength = 64;

    public MeterReadingValidator()
    {
        RuleFor(reading => reading.Location)
            .NotEmpty()
            .MaximumLength(MaxLocationLength);

        RuleFor(reading => reading).SetInheritanceValidator(validators =>
        {
            validators.Add(new EnergyReadingValidator());
            validators.Add(new AirQualityReadingValidator());
        });
    }

    private sealed class EnergyReadingValidator : AbstractValidator<EnergyReading>
    {
        public EnergyReadingValidator()
        {
            RuleFor(reading => reading.Energy)
                .Must(double.IsFinite).WithMessage("Energy must be a finite number.")
                .InclusiveBetween(0, 100_000);
        }
    }

    private sealed class AirQualityReadingValidator : AbstractValidator<AirQualityReading>
    {
        public AirQualityReadingValidator()
        {
            RuleFor(reading => reading.Co2).InclusiveBetween(0, 10_000);
            RuleFor(reading => reading.Pm25).InclusiveBetween(0, 1_000);
            RuleFor(reading => reading.Humidity).InclusiveBetween(0, 100);
        }
    }
}
