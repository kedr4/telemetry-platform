using FluentValidation;

namespace Telemetry.Gateway.Application.Meters.GetMeters;

internal sealed class GetMetersQueryValidator : AbstractValidator<GetMetersQuery>
{
    public GetMetersQueryValidator() => RuleForEach(query => query.Types).IsInEnum();
}
