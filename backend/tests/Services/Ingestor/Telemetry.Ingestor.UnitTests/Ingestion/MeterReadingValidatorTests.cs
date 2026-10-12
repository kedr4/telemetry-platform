using Telemetry.Contracts.Readings;
using Telemetry.Ingestor.Ingestion.Validation;

namespace Telemetry.Ingestor.UnitTests.Ingestion;

public class MeterReadingValidatorTests
{
    private readonly MeterReadingValidator _validator = new();

    public static TheoryData<MeterReading> ValidReadings =>
    [
        new EnergyReading("Kitchen", 0),
        new EnergyReading("Kitchen", 986.57),
        new AirQualityReading("Garage", 300, 8, 22),
        new AirQualityReading("Garage", 10_000, 1_000, 100),
        new MotionReading("Office", MotionDetected: true),
    ];

    public static TheoryData<MeterReading, string> InvalidReadings =>
    new()
    {
        { new EnergyReading("Kitchen", -1), nameof(EnergyReading.Energy) },
        { new EnergyReading("Kitchen", double.NaN), nameof(EnergyReading.Energy) },
        { new EnergyReading("Kitchen", double.PositiveInfinity), nameof(EnergyReading.Energy) },
        { new AirQualityReading("Garage", -5, 26, 68), nameof(AirQualityReading.Co2) },
        { new AirQualityReading("Garage", 543, 5_000, 68), nameof(AirQualityReading.Pm25) },
        { new AirQualityReading("Garage", 543, 26, 101), nameof(AirQualityReading.Humidity) },
        { new MotionReading(string.Empty, MotionDetected: true), nameof(MeterReading.Location) },
        { new MotionReading(new string('x', MeterReadingValidator.MaxLocationLength + 1), MotionDetected: true), nameof(MeterReading.Location) },
    };

    [Theory]
    [MemberData(nameof(ValidReadings))]
    public void Validate_PlausibleReading_IsValid(MeterReading reading)
    {
        _validator.Validate(reading).IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidReadings))]
    public void Validate_ImplausibleReading_FailsOnProperty(MeterReading reading, string property)
    {
        var result = _validator.Validate(reading);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName.EndsWith(property, StringComparison.Ordinal));
    }
}
