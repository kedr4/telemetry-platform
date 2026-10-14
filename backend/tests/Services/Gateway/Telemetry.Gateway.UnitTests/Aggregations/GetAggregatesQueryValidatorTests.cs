using Telemetry.Gateway.Application.Aggregations.GetAggregates;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Metrics;

namespace Telemetry.Gateway.UnitTests.Aggregations;

public class GetAggregatesQueryValidatorTests
{
    private readonly GetAggregatesQueryValidator _validator = new();

    [Theory]
    [InlineData(new[] { AggregationDimension.Location }, Metric.Co2, null)]
    [InlineData(new[] { AggregationDimension.Type }, null, null)]
    [InlineData(new[] { AggregationDimension.Time }, Metric.Energy, TimeBucket.Hour)]
    [InlineData(new[] { AggregationDimension.Time, AggregationDimension.Location }, Metric.Motion, TimeBucket.Minute)]
    public void Validate_ValidCombination_Succeeds(AggregationDimension[] groupBy, Metric? metric, TimeBucket? bucket)
    {
        _validator.Validate(new GetAggregatesQuery(groupBy, metric, bucket, ReadingFilter.None)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NoDimension_Fails()
    {
        Errors([], Metric.Co2, null).ShouldContain(nameof(GetAggregatesQuery.GroupBy));
    }

    [Fact]
    public void Validate_DuplicateDimension_Fails()
    {
        Errors([AggregationDimension.Location, AggregationDimension.Location], null, null)
            .ShouldContain(nameof(GetAggregatesQuery.GroupBy));
    }

    [Fact]
    public void Validate_TimeWithoutBucket_Fails()
    {
        Errors([AggregationDimension.Time], Metric.Co2, null).ShouldContain(nameof(GetAggregatesQuery.Bucket));
    }

    [Fact]
    public void Validate_BucketWithoutTime_Fails()
    {
        Errors([AggregationDimension.Location], Metric.Co2, TimeBucket.Hour).ShouldContain(nameof(GetAggregatesQuery.Bucket));
    }

    [Fact]
    public void Validate_UndefinedMetric_Fails()
    {
        Errors([AggregationDimension.Location], (Metric)42, null).ShouldContain(nameof(GetAggregatesQuery.Metric));
    }

    private List<string> Errors(AggregationDimension[] groupBy, Metric? metric, TimeBucket? bucket) =>
        _validator.Validate(new GetAggregatesQuery(groupBy, metric, bucket, ReadingFilter.None)).Errors
            .Select(error => error.PropertyName)
            .ToList();
}
