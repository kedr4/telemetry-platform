using MediatR;
using Telemetry.Gateway.Application.Abstractions;

namespace Telemetry.Gateway.Application.Meters.GetLocations;

internal sealed class GetLocationsQueryHandler(ITelemetryReadStore store) : IRequestHandler<GetLocationsQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetLocationsQuery request, CancellationToken cancellationToken) =>
        store.GetLocationsAsync(cancellationToken);
}
