using System.Text.Json;
using System.Text.Json.Serialization;

namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>
/// JSON (de)serialization of broker messages. Strict: nullable annotations and required constructor
/// parameters are enforced so malformed messages fail fast and can be dead-lettered.
/// </summary>
public static class MessageSerializer
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static byte[] Serialize<TMessage>(TMessage message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, Options);

    /// <exception cref="JsonException">The body is not a valid <typeparamref name="TMessage"/>.</exception>
    public static TMessage Deserialize<TMessage>(ReadOnlyMemory<byte> body) =>
        JsonSerializer.Deserialize<TMessage>(body.Span, Options)
        ?? throw new JsonException($"Message body does not contain a {typeof(TMessage).Name}.");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
