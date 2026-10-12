using Microsoft.Extensions.Options;
using Telemetry.BuildingBlocks.Correlation;

namespace Telemetry.Ingestor.Ingestion;

/// <summary>
/// Runs ingestion cycles forever. Waits <see cref="IngestionOptions.PollInterval"/> between cycles, or longer
/// when the API asks for it (Retry-After / open circuit), capped by <see cref="IngestionOptions.MaxBackoff"/>.
/// </summary>
internal sealed partial class IngestionWorker(
    BatchIngestionService ingestion,
    IOptions<IngestionOptions> options,
    TimeProvider timeProvider,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    internal static TimeSpan GetDelay(IngestionResult result, IngestionOptions settings)
    {
        if (result.RetryAfter is not { } retryAfter || retryAfter <= settings.PollInterval)
        {
            return settings.PollInterval;
        }

        return retryAfter < settings.MaxBackoff ? retryAfter : settings.MaxBackoff;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        LogStarted(settings.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = settings.PollInterval;

            try
            {
                using (CorrelationContext.BeginScope(CorrelationContext.NewId()))
                {
                    var result = await ingestion.IngestAsync(stoppingToken);
                    delay = GetDelay(result, settings);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A bug in one cycle must never stop ingestion altogether.
                LogCycleCrashed(ex);
            }

            try
            {
                await Task.Delay(delay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ingestion started; polling every {PollInterval}")]
    private partial void LogStarted(TimeSpan pollInterval);

    [LoggerMessage(Level = LogLevel.Error, Message = "Ingestion cycle crashed unexpectedly")]
    private partial void LogCycleCrashed(Exception exception);
}
