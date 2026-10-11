using Serilog.Context;

namespace Telemetry.BuildingBlocks.Correlation;

/// <summary>
/// Ambient correlation identifier that flows through HTTP requests, log events and broker messages.
/// </summary>
public static class CorrelationContext
{
    public const string HeaderName = "X-Correlation-ID";
    public const string LogPropertyName = "CorrelationId";

    private const int MaxLength = 64;

    private static readonly AsyncLocal<string?> Current = new();

    /// <summary>Gets the correlation identifier of the current logical operation, if any.</summary>
    public static string? CorrelationId => Current.Value;

    public static string NewId() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Accepts only short identifiers made of letters, digits, '-' and '_' to prevent log injection.
    /// </summary>
    public static bool IsValid(string? correlationId) =>
        !string.IsNullOrEmpty(correlationId)
        && correlationId.Length <= MaxLength
        && correlationId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    /// <summary>
    /// Sets the correlation identifier (and the matching log property) until the returned scope is disposed.
    /// </summary>
    public static IDisposable BeginScope(string correlationId)
    {
        var previous = Current.Value;
        Current.Value = correlationId;
        var logScope = LogContext.PushProperty(LogPropertyName, correlationId);
        return new Scope(previous, logScope);
    }

    private sealed class Scope(string? previous, IDisposable logScope) : IDisposable
    {
        public void Dispose()
        {
            logScope.Dispose();
            Current.Value = previous;
        }
    }
}
