using MediatR;
using Telemetry.Gateway.Domain.Meters;

namespace Telemetry.Gateway.Application.Meters.GetMeters;

/// <summary>All known meters, optionally restricted to some types.</summary>
/// <param name="Types">Meter types to include; empty for all.</param>
public sealed record GetMetersQuery(IReadOnlyCollection<MeterType> Types) : IRequest<IReadOnlyList<Meter>>;
