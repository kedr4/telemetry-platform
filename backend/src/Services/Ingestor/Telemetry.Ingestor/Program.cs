using Telemetry.BuildingBlocks.Hosting;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Ingestor;

return await ServiceHost.RunAsync(async () =>
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();
    builder.Services.AddRabbitMqMessaging(builder.Configuration);
    builder.Services.AddIngestor(builder.Configuration);

    var app = builder.Build();

    app.UseServiceDefaults();
    app.MapIngestorEndpoints();

    await app.RunAsync();
});
