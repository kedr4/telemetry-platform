using System.Text.Json;

namespace Telemetry.Ingestor.WeakApi;

/// <summary>
/// A meter exactly as the upstream API returns it. Nothing is trusted: every member may be missing
/// or of the wrong kind, which is why the payload stays an untyped <see cref="JsonElement"/>.
/// </summary>
/// <param name="Type">Meter type, e.g. <c>energy</c>.</param>
/// <param name="Name">Location of the meter, e.g. <c>Kitchen</c>.</param>
/// <param name="Payload">Type-specific measurement values.</param>
public sealed record WeakApiMeterDto(string? Type, string? Name, JsonElement Payload);
