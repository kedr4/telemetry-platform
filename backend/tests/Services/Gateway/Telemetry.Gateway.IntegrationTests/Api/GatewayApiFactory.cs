using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Telemetry.Gateway.IntegrationTests.Infrastructure;

namespace Telemetry.Gateway.IntegrationTests.Api;

/// <summary>The real gateway application, connected to the test database.</summary>
public sealed class GatewayApiFactory(GatewayDatabaseFixture database) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Telemetry", database.ConnectionString);
}
