using Telemetry.BuildingBlocks.Hosting;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Processor;
using Telemetry.Processor.Persistence;

return await ServiceHost.RunAsync(async () =>
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    // Persistence first: its migrator must finish before the consumer starts.
    builder.Services.AddProcessorPersistence(builder.Configuration);
    builder.Services.AddRabbitMqMessaging(builder.Configuration);
    builder.Services.AddProcessor();

    var app = builder.Build();

    app.UseServiceDefaults();

    await app.RunAsync();
});
