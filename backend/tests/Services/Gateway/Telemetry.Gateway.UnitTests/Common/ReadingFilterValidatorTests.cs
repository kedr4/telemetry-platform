using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.UnitTests.Common;

public class ReadingFilterValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly ReadingFilterValidator _validator = new();

    [Fact]
    public void Validate_NoRestrictions_Succeeds()
    {
        _validator.Validate(ReadingFilter.None).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_FromAfterTo_Fails()
    {
        _validator.Validate(new ReadingFilter([], [], From: Now, To: Now.AddHours(-1))).Errors
            .ShouldContain(error => error.PropertyName == nameof(ReadingFilter.From));
    }

    [Fact]
    public void Validate_EqualFromAndTo_Fails()
    {
        _validator.Validate(new ReadingFilter([], [], From: Now, To: Now)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_TooManyLocations_Fails()
    {
        var locations = Enumerable.Range(0, ReadingFilterValidator.MaxLocations + 1).Select(i => $"Room {i}").ToList();

        _validator.Validate(new ReadingFilter(locations, [])).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a-location-name-that-is-much-longer-than-sixty-four-characters-allowed")]
    public void Validate_InvalidLocation_Fails(string location)
    {
        _validator.Validate(new ReadingFilter([location], [])).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_UndefinedMeterType_Fails()
    {
        _validator.Validate(new ReadingFilter([], [(MeterType)99])).IsValid.ShouldBeFalse();
    }
}
