using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace Telemetry.Processor.Persistence;

/// <summary>
/// Applies pending migrations before anything else starts (hosted services start in registration order),
/// so the consumer never sees an outdated schema. Retries while the database is still starting.
/// Concurrent instances are safe: EF Core serializes migrations with a database lock.
/// </summary>
internal sealed partial class DatabaseMigrator(IServiceScopeFactory scopeFactory, ILogger<DatabaseMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                MaxRetryAttempts = 10,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(10),
                OnRetry = args =>
                {
                    LogRetry(args.Outcome.Exception, args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        await pipeline.ExecuteAsync(
            async token =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ProcessorDbContext>();
                var pending = (await db.Database.GetPendingMigrationsAsync(token)).ToList();
                await db.Database.MigrateAsync(token);
                LogMigrated(pending.Count, pending);
            },
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Database schema is up to date ({Count} migrations applied: {Migrations})")]
    private partial void LogMigrated(int count, IReadOnlyList<string> migrations);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Migrating the database failed (attempt {Attempt}); retrying")]
    private partial void LogRetry(Exception? exception, int attempt);
}
