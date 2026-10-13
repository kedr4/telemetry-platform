using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Contracts.Messaging;
using Telemetry.Processor.Diagnostics;
using Telemetry.Processor.Messaging;

namespace Telemetry.Processor;

public static class ProcessorServiceCollectionExtensions
{
    public static IServiceCollection AddProcessor(this IServiceCollection services)
    {
        services.AddMetrics();
        services.AddSingleton<ProcessorMetrics>();
        services.AddMessageConsumer<MeterBatchFetched, MeterBatchFetchedHandler>(QueueNames.ProcessorMeterBatches);

        return services;
    }
}
