using Microsoft.AspNetCore.Http;
using Telemetry.BuildingBlocks.Correlation;

namespace Telemetry.BuildingBlocks.UnitTests.Correlation;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ValidIncomingId_IsPropagatedAndEchoed()
    {
        var (seenInPipeline, context) = await InvokeAsync("abc-123_DEF");

        seenInPipeline.ShouldBe("abc-123_DEF");
        context.Response.Headers[CorrelationContext.HeaderName].ToString().ShouldBe("abc-123_DEF");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("contains spaces and\nnew lines")]
    [InlineData("an-identifier-that-is-far-too-long-to-be-accepted-as-correlation-id-xx")]
    public async Task InvokeAsync_MissingOrInvalidId_GeneratesNewId(string? incoming)
    {
        var (seenInPipeline, context) = await InvokeAsync(incoming);

        seenInPipeline.ShouldNotBeNullOrEmpty();
        seenInPipeline.ShouldNotBe(incoming);
        CorrelationContext.IsValid(seenInPipeline).ShouldBeTrue();
        context.Response.Headers[CorrelationContext.HeaderName].ToString().ShouldBe(seenInPipeline);
    }

    [Fact]
    public async Task InvokeAsync_AfterPipeline_RestoresPreviousContext()
    {
        await InvokeAsync("abc");

        CorrelationContext.CorrelationId.ShouldBeNull();
    }

    private static async Task<(string? SeenInPipeline, HttpContext Context)> InvokeAsync(string? incomingId)
    {
        var context = new DefaultHttpContext();
        if (incomingId is not null)
        {
            context.Request.Headers[CorrelationContext.HeaderName] = incomingId;
        }

        string? seen = null;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            seen = CorrelationContext.CorrelationId;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);
        return (seen, context);
    }
}
