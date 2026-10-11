namespace Telemetry.BuildingBlocks.Messaging;

/// <summary>
/// Signals that a message can never be processed successfully (e.g. it violates business rules),
/// so it is dead-lettered immediately instead of being retried.
/// </summary>
public sealed class NonRetryableMessageException : Exception
{
    public NonRetryableMessageException()
    {
    }

    public NonRetryableMessageException(string message)
        : base(message)
    {
    }

    public NonRetryableMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
