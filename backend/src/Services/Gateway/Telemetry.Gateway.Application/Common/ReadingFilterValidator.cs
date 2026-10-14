using FluentValidation;

namespace Telemetry.Gateway.Application.Common;

internal sealed class ReadingFilterValidator : AbstractValidator<ReadingFilter>
{
    public const int MaxLocations = 50;
    public const int MaxLocationLength = 64;

    public ReadingFilterValidator()
    {
        RuleFor(filter => filter.Locations)
            .Must(locations => locations.Count <= MaxLocations)
            .WithMessage($"At most {MaxLocations} locations can be requested.");
        RuleForEach(filter => filter.Locations).NotEmpty().MaximumLength(MaxLocationLength);
        RuleForEach(filter => filter.Types).IsInEnum();
        RuleFor(filter => filter.From)
            .LessThan(filter => filter.To)
            .When(filter => filter.From.HasValue && filter.To.HasValue)
            .WithMessage("'From' must be earlier than 'To'.");
    }
}
