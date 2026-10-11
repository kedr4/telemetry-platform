using System.Globalization;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Telemetry.BuildingBlocks.Hosting;

/// <summary>
/// Runs a service entry point with a bootstrap logger so that start-up failures are always logged.
/// </summary>
public static class ServiceHost
{
    public static async Task<int> RunAsync(Func<Task> run)
    {
        ArgumentNullException.ThrowIfNull(run);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateBootstrapLogger();

        try
        {
            await run();
            return 0;
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "Service terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
