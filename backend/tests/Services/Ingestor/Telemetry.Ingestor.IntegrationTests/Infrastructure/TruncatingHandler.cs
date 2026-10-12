using System.Net;
using System.Text;

namespace Telemetry.Ingestor.IntegrationTests.Infrastructure;

/// <summary>
/// Reproduces WeakApp's "data corrupted" failure exactly: <c>200 OK</c> with a chunked body that is aborted
/// after the first chunk. The first <paramref name="truncatedResponses"/> requests are truncated, later ones succeed.
/// </summary>
internal sealed class TruncatingHandler(int truncatedResponses) : HttpMessageHandler
{
    private int _requests;

    public int Requests => Volatile.Read(ref _requests);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestNumber = Interlocked.Increment(ref _requests);
        HttpContent content = requestNumber <= truncatedResponses
            ? new StreamContent(new AbortedStream("""{"error": "data corrupted"}"""))
            : new StringContent(FakeWeakApp.ValidBody, Encoding.UTF8, "application/json");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }

    /// <summary>Yields the given bytes, then fails like a connection closed before the terminating chunk.</summary>
    private sealed class AbortedStream(string partialBody) : MemoryStream(Encoding.UTF8.GetBytes(partialBody))
    {
        public override int Read(byte[] buffer, int offset, int count) =>
            ReadOrAbort(base.Read(buffer, offset, count));

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadOrAbort(await base.ReadAsync(buffer.AsMemory(offset, count), cancellationToken));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ReadOrAbort(await base.ReadAsync(buffer, cancellationToken));

        private static int ReadOrAbort(int read) =>
            read > 0 ? read : throw new IOException("The response ended prematurely.");
    }
}
