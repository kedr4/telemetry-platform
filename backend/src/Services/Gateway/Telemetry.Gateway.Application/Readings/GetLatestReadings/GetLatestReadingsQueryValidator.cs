using FluentValidation;
using Telemetry.Gateway.Application.Common;

namespace Telemetry.Gateway.Application.Readings.GetLatestReadings;

internal sealed class GetLatestReadingsQueryValidator : AbstractValidator<GetLatestReadingsQuery>
{
    public GetLatestReadingsQueryValidator() =>
        RuleFor(query => query.Filter).SetValidator(new ReadingFilterValidator());
}
