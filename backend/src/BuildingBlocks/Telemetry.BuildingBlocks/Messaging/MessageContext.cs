namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>Broker metadata of the message being handled.</summary>
/// <param name="MessageId">Identifier assigned by the publisher.</param>
/// <param name="CorrelationId">Correlation identifier of the originating operation.</param>
/// <param name="Redelivered">Whether the broker delivered this message before.</param>
public sealed record MessageContext(string? MessageId, string CorrelationId, bool Redelivered);
