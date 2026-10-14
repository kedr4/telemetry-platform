using Telemetry.Gateway.Application.Readings;

namespace Telemetry.Gateway.UnitTests.Readings;

public class ReadingCursorTests
{
    [Fact]
    public void Encode_ThenDecode_RoundTrips()
    {
        var cursor = new ReadingCursor(new DateTimeOffset(2026, 10, 18, 12, 30, 15, 123, TimeSpan.Zero), 4711);

        ReadingCursor.TryDecode(cursor.Encode(), out var decoded).ShouldBeTrue();

        decoded.ShouldBe(cursor);
    }

    [Fact]
    public void Encode_IsUrlSafe()
    {
        var encoded = new ReadingCursor(DateTimeOffset.MaxValue, long.MaxValue).Encode();

        encoded.ShouldNotContain("+");
        encoded.ShouldNotContain("/");
        encoded.ShouldNotContain("=");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("MTIz")] // "123": no separator
    [InlineData("YWJjOjEy")] // "abc:12": ticks not numeric
    [InlineData("LTE6MTI")] // "-1:12": negative ticks
    [InlineData("NDAwMDAwMDAwMDAwMDAwMDAwMDox")] // "4000000000000000000:1": ticks beyond DateTimeOffset.MaxValue
    public void TryDecode_InvalidCursor_ReturnsFalse(string? value)
    {
        ReadingCursor.TryDecode(value, out _).ShouldBeFalse();
    }
}
