using Telemetry.BuildingBlocks.Hosting;
using Telemetry.Gateway.Api;
using Telemetry.Gateway.Application;
using Telemetry.Gateway.Infrastructure;

return await ServiceHost.RunAsync(async () =>
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();
    builder.Services.AddGatewayApplication();
    builder.Services.AddGatewayInfrastructure(builder.Configuration);
    builder.AddGatewayApi();

    var app = builder.Build();

    app.UseServiceDefaults();
    app.UseGatewayApi();

    await app.RunAsync();
});
