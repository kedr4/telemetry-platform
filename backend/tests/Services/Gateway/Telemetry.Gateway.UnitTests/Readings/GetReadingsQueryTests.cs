using Telemetry.Gateway.Application.Abstractions;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Application.Readings;
using Telemetry.Gateway.Application.Readings.GetReadings;
using Telemetry.Gateway.Domain.Meters;
using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.UnitTests.Readings;

public class GetReadingsQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly GetReadingsQueryValidator _validator = new();
    private readonly ITelemetryReadStore _store = Substitute.For<ITelemetryReadStore>();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(GetReadingsQuery.MaxPageSize + 1)]
    public void Validate_PageSizeOutOfRange_Fails(int first)
    {
        _validator.Validate(new GetReadingsQuery(ReadingFilter.None, first)).Errors
            .ShouldContain(error => error.PropertyName == nameof(GetReadingsQuery.First));
    }

    [Fact]
    public void Validate_MalformedCursor_Fails()
    {
        _validator.Validate(new GetReadingsQuery(ReadingFilter.None, After: "garbage!")).Errors
            .ShouldContain(error => error.PropertyName == nameof(GetReadingsQuery.After));
    }

    [Fact]
    public void Validate_ValidQuery_Succeeds()
    {
        var after = new ReadingCursor(Now, 1).Encode();

        _validator.Validate(new GetReadingsQuery(new ReadingFilter(["Kitchen"], [MeterType.Energy], Now.AddDays(-1), Now), 100, after))
            .IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_FirstPage_PassesNoCursor_AndReturnsCursorOfLastItem()
    {
        IReadOnlyList<Reading> items = [Energy(2, Now), Energy(1, Now.AddMinutes(-1))];
        _store.GetReadingsAsync(ReadingFilter.None, null, 2, Arg.Any<CancellationToken>())
            .Returns(new ReadingSlice(items, TotalCount: 5, HasMore: true));

        var page = await new GetReadingsQueryHandler(_store).Handle(new GetReadingsQuery(ReadingFilter.None, 2), CancellationToken);

        page.Items.ShouldBe(items);
        page.TotalCount.ShouldBe(5);
        page.HasNextPage.ShouldBeTrue();
        ReadingCursor.TryDecode(page.EndCursor, out var end).ShouldBeTrue();
        end.ShouldBe(new ReadingCursor(Now.AddMinutes(-1), 1));
    }

    [Fact]
    public async Task Handle_NextPage_PassesDecodedCursorToStore()
    {
        var cursor = new ReadingCursor(Now, 42);
        _store.GetReadingsAsync(Arg.Any<ReadingFilter>(), Arg.Any<ReadingCursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ReadingSlice([], TotalCount: 0, HasMore: false));

        var page = await new GetReadingsQueryHandler(_store)
            .Handle(new GetReadingsQuery(ReadingFilter.None, 10, cursor.Encode()), CancellationToken);

        await _store.Received(1).GetReadingsAsync(ReadingFilter.None, cursor, 10, Arg.Any<CancellationToken>());
        page.EndCursor.ShouldBeNull();
        page.HasNextPage.ShouldBeFalse();
    }

    private static EnergyReading Energy(long id, DateTimeOffset at) => new(id, 1, "Kitchen", at, 100);
}
