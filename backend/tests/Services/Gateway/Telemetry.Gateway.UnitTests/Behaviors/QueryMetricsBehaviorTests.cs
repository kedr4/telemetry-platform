using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Telemetry.Gateway.Application.Behaviors;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Application.Diagnostics;

namespace Telemetry.Gateway.UnitTests.Behaviors;

public sealed class QueryMetricsBehaviorTests : IDisposable
{
    private const string Instrument = "telemetry.gateway.query.duration";

    private readonly ServiceProvider _services = new ServiceCollection().AddMetrics().BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private IMeterFactory MeterFactory => _services.GetRequiredService<IMeterFactory>();

    [Fact]
    public async Task Handle_Success_RecordsQueryNameAndSuccess()
    {
        using var collector = new MetricCollector<double>(MeterFactory, GatewayMetrics.MeterName, Instrument);
        var behavior = CreateBehavior();

        var response = await behavior.Handle(new TestQuery(), _ => Task.FromResult("ok"), CancellationToken);

        response.ShouldBe("ok");
        var measurement = collector.LastMeasurement.ShouldNotBeNull();
        measurement.Tags["query"].ShouldBe(nameof(TestQuery));
        measurement.Tags["outcome"].ShouldBe("success");
        measurement.Value.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_ValidationFailure_RecordsInvalid()
    {
        using var collector = new MetricCollector<double>(MeterFactory, GatewayMetrics.MeterName, Instrument);
        var behavior = CreateBehavior();
        RequestHandlerDelegate<string> next = _ => throw new RequestValidationException(
            new Dictionary<string, string[]> { ["Name"] = ["required"] });

        await Should.ThrowAsync<RequestValidationException>(() => behavior.Handle(new TestQuery(), next, CancellationToken));

        collector.LastMeasurement.ShouldNotBeNull().Tags["outcome"].ShouldBe("invalid");
    }

    [Fact]
    public async Task Handle_Failure_RecordsError()
    {
        using var collector = new MetricCollector<double>(MeterFactory, GatewayMetrics.MeterName, Instrument);
        var behavior = CreateBehavior();
        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("database down");

        await Should.ThrowAsync<InvalidOperationException>(() => behavior.Handle(new TestQuery(), next, CancellationToken));

        collector.LastMeasurement.ShouldNotBeNull().Tags["outcome"].ShouldBe("error");
    }

    public void Dispose() => _services.Dispose();

    private QueryMetricsBehavior<TestQuery, string> CreateBehavior() => new(new GatewayMetrics(MeterFactory));

    public sealed record TestQuery : IRequest<string>;
}
