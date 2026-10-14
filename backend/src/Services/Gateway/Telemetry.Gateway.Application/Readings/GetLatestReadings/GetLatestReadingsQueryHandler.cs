using MediatR;
using Telemetry.Gateway.Application.Abstractions;
using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Application.Readings.GetLatestReadings;

internal sealed class GetLatestReadingsQueryHandler(ITelemetryReadStore store)
    : IRequestHandler<GetLatestReadingsQuery, IReadOnlyList<Reading>>
{
    public Task<IReadOnlyList<Reading>> Handle(GetLatestReadingsQuery request, CancellationToken cancellationToken) =>
        store.GetLatestReadingsAsync(request.Filter, cancellationToken);
}
