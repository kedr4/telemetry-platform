using Microsoft.AspNetCore.Http;
using Serilog.Events;
using Telemetry.BuildingBlocks.Hosting;

namespace Telemetry.BuildingBlocks.UnitTests.Hosting;

public class ServiceDefaultsTests
{
    [Theory]
    [InlineData("/health/live", 200, LogEventLevel.Verbose)]
    [InlineData("/metrics", 200, LogEventLevel.Verbose)]
    [InlineData("/api/v1/readings", 200, LogEventLevel.Information)]
    [InlineData("/api/v1/readings", 404, LogEventLevel.Information)]
    [InlineData("/api/v1/readings", 500, LogEventLevel.Error)]
    [InlineData("/health/ready", 503, LogEventLevel.Error)]
    public void GetRequestLogLevel_DependsOnPathAndStatus(string path, int statusCode, LogEventLevel expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.StatusCode = statusCode;

        ServiceDefaultsExtensions.GetRequestLogLevel(context, 1, exception: null).ShouldBe(expected);
    }

    [Fact]
    public void GetRequestLogLevel_WithException_IsError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";

        ServiceDefaultsExtensions.GetRequestLogLevel(context, 1, new InvalidOperationException()).ShouldBe(LogEventLevel.Error);
    }
}
