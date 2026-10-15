using Telemetry.BuildingBlocks.Hosting;
using Telemetry.BuildingBlocks.Messaging;
using Telemetry.Notifications;

return await ServiceHost.RunAsync(async () =>
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();
    builder.Services.AddRabbitMqMessaging(builder.Configuration);
    builder.AddNotifications();

    var app = builder.Build();

    app.UseServiceDefaults();
    app.UseNotifications();

    await app.RunAsync();
});
