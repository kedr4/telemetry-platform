using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Telemetry.Gateway.Application.Readings;

/// <summary>
/// Opaque keyset-pagination cursor: the position of the last reading of a page in the
/// (RecordedAt desc, Id desc) order. Stable while new readings arrive, unlike offsets.
/// </summary>
/// <param name="RecordedAt">Timestamp of the last returned reading.</param>
/// <param name="Id">Identifier of the last returned reading (tie-breaker).</param>
public readonly record struct ReadingCursor(DateTimeOffset RecordedAt, long Id)
{
    public string Encode() =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{RecordedAt.UtcTicks}:{Id}")));

    public static bool TryDecode(string? value, out ReadingCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrEmpty(value) || !Base64Url.IsValid(value))
        {
            return false;
        }

        var parts = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value)).Split(':');
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            || ticks > DateTimeOffset.MaxValue.UtcTicks)
        {
            return false;
        }

        cursor = new ReadingCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
