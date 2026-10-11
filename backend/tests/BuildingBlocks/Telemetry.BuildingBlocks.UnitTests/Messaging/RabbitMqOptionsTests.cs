using System.ComponentModel.DataAnnotations;
using Telemetry.BuildingBlocks.Messaging;

namespace Telemetry.BuildingBlocks.UnitTests.Messaging;

public class RabbitMqOptionsTests
{
    [Fact]
    public void Validate_CompleteOptions_Succeeds()
    {
        var options = new RabbitMqOptions { UserName = "user", Password = "secret" };

        Validate(options).ShouldBeEmpty();
    }

    [Fact]
    public void Validate_MissingCredentials_Fails()
    {
        var options = new RabbitMqOptions();

        Validate(options).SelectMany(r => r.MemberNames).ShouldBe(
            [nameof(RabbitMqOptions.UserName), nameof(RabbitMqOptions.Password)],
            ignoreOrder: true);
    }

    [Fact]
    public void Validate_PortOutOfRange_Fails()
    {
        var options = new RabbitMqOptions { UserName = "user", Password = "secret", Port = 0 };

        Validate(options).SelectMany(r => r.MemberNames).ShouldBe([nameof(RabbitMqOptions.Port)]);
    }

    private static List<ValidationResult> Validate(RabbitMqOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
