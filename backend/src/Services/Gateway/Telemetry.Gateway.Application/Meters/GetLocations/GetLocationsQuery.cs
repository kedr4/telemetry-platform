using MediatR;

namespace Telemetry.Gateway.Application.Meters.GetLocations;

/// <summary>All locations that have at least one meter, alphabetically.</summary>
public sealed record GetLocationsQuery : IRequest<IReadOnlyList<string>>;
