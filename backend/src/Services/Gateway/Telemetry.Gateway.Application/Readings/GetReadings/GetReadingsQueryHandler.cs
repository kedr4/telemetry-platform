using MediatR;
using Telemetry.Gateway.Application.Abstractions;

namespace Telemetry.Gateway.Application.Readings.GetReadings;

internal sealed class GetReadingsQueryHandler(ITelemetryReadStore store) : IRequestHandler<GetReadingsQuery, ReadingPage>
{
    public async Task<ReadingPage> Handle(GetReadingsQuery request, CancellationToken cancellationToken)
    {
        ReadingCursor? after = ReadingCursor.TryDecode(request.After, out var cursor) ? cursor : null;

        var slice = await store.GetReadingsAsync(request.Filter, after, request.First, cancellationToken);

        var endCursor = slice.Items.Count > 0
            ? new ReadingCursor(slice.Items[^1].RecordedAt, slice.Items[^1].Id).Encode()
            : null;
        return new ReadingPage(slice.Items, slice.TotalCount, slice.HasMore, endCursor);
    }
}
