using System.Collections.Concurrent;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;

namespace Telemetry.BuildingBlocks.IntegrationTests.Infrastructure;

/// <summary>Records what the consumer delivered and lets a test make the handler fail.</summary>
public sealed class HandlerProbe
{
    private int _attempts;

    public ConcurrentQueue<(ReadingsPersisted Message, MessageContext Context)> Handled { get; } = new();

    public int Attempts => Volatile.Read(ref _attempts);

    public Func<Exception>? Failure { get; init; }

    internal void RecordAttempt() => Interlocked.Increment(ref _attempts);
}
