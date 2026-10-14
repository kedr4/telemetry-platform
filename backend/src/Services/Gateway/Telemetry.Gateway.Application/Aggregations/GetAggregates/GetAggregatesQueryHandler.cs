using MediatR;
using Telemetry.Gateway.Application.Abstractions;
using Telemetry.Gateway.Domain.Aggregations;

namespace Telemetry.Gateway.Application.Aggregations.GetAggregates;

internal sealed class GetAggregatesQueryHandler(ITelemetryReadStore store)
    : IRequestHandler<GetAggregatesQuery, IReadOnlyList<AggregateResult>>
{
    public Task<IReadOnlyList<AggregateResult>> Handle(GetAggregatesQuery request, CancellationToken cancellationToken) =>
        store.GetAggregatesAsync(
            new AggregationSpec(request.GroupBy, request.Metric, request.Bucket, request.Filter),
            cancellationToken);
}
