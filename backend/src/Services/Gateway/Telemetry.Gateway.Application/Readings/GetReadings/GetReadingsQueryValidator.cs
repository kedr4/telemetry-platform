using FluentValidation;
using Telemetry.Gateway.Application.Common;

namespace Telemetry.Gateway.Application.Readings.GetReadings;

internal sealed class GetReadingsQueryValidator : AbstractValidator<GetReadingsQuery>
{
    public GetReadingsQueryValidator()
    {
        RuleFor(query => query.Filter).SetValidator(new ReadingFilterValidator());
        RuleFor(query => query.First).InclusiveBetween(1, GetReadingsQuery.MaxPageSize);
        RuleFor(query => query.After)
            .Must(after => ReadingCursor.TryDecode(after, out _))
            .When(query => query.After is not null)
            .WithMessage("'After' is not a valid cursor.");
    }
}
