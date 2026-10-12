namespace Telemetry.Testing;

/// <summary>Polls a condition until it holds, for asserting on asynchronous side effects.</summary>
public static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    public static Task TrueAsync(Func<bool> condition, CancellationToken cancellationToken) =>
        TrueAsync(() => Task.FromResult(condition()), cancellationToken);

    public static async Task TrueAsync(Func<Task<bool>> condition, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(DefaultTimeout);

        while (!await condition())
        {
            if (timeout.IsCancellationRequested)
            {
                throw new TimeoutException($"Condition was not met within {DefaultTimeout}.");
            }

            await Task.Delay(PollInterval, CancellationToken.None);
        }
    }
}
