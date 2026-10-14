using MediatR;
using Telemetry.Gateway.Application.Abstractions;
using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Application.Meters.GetMeters;

internal sealed class GetMetersQueryHandler(ITelemetryReadStore store) : IRequestHandler<GetMetersQuery, IReadOnlyList<Meter>>
{
    public Task<IReadOnlyList<Meter>> Handle(GetMetersQuery request, CancellationToken cancellationToken) =>
        store.GetMetersAsync(request.Types, cancellationToken);
}
