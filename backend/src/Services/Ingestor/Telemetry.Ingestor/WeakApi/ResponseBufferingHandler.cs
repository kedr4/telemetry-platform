namespace Telemetry.Ingestor.WeakApi;

/// <summary>
/// Reads the whole response body inside each attempt. WeakApp sometimes sends <c>200 OK</c> and then drops
/// the connection mid-body; without buffering here the body would only be read after the resilience
/// pipeline has already accepted the response, so the truncation could not be retried.
/// Registered as the innermost handler, i.e. inside the per-attempt timeout.
/// </summary>
internal sealed class ResponseBufferingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        try
        {
            await response.Content.LoadIntoBufferAsync(cancellationToken);
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
