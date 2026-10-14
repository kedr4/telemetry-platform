using FluentValidation;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Domain.Aggregations;

namespace Telemetry.Gateway.Application.Aggregations.GetAggregates;

internal sealed class GetAggregatesQueryValidator : AbstractValidator<GetAggregatesQuery>
{
    public GetAggregatesQueryValidator()
    {
        RuleFor(query => query.Filter).SetValidator(new ReadingFilterValidator());

        RuleFor(query => query.GroupBy)
            .NotEmpty()
            .Must(groupBy => groupBy.Distinct().Count() == groupBy.Count)
            .WithMessage("Each dimension can be grouped by only once.");
        RuleForEach(query => query.GroupBy).IsInEnum();

        RuleFor(query => query.Metric).IsInEnum().When(query => query.Metric.HasValue);

        RuleFor(query => query.Bucket)
            .NotNull()
            .When(query => query.GroupBy.Contains(AggregationDimension.Time))
            .WithMessage("'Bucket' is required when grouping by time.");
        RuleFor(query => query.Bucket)
            .Null()
            .When(query => !query.GroupBy.Contains(AggregationDimension.Time))
            .WithMessage("'Bucket' can only be used when grouping by time.");
        RuleFor(query => query.Bucket).IsInEnum().When(query => query.Bucket.HasValue);
    }
}
