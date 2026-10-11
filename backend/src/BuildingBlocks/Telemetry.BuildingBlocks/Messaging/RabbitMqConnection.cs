using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace Telemetry.BuildingBlocks.Messaging;

internal sealed partial class RabbitMqConnection : IRabbitMqConnection, IAsyncDisposable
{
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions _options;
    private readonly string _clientName;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly ResiliencePipeline _connectPipeline;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(
        IOptions<RabbitMqOptions> options,
        IHostEnvironment environment,
        ILogger<RabbitMqConnection> logger)
    {
        _options = options.Value;
        _clientName = environment.ApplicationName;
        _logger = logger;
        _connectPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                // The broker may still be starting; keep trying until the service is stopped.
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                MaxRetryAttempts = int.MaxValue,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30),
                OnRetry = args =>
                {
                    LogConnectRetry(args.Outcome.Exception, args.AttemptNumber + 1, args.RetryDelay);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    public bool IsConnected => _connection?.IsOpen == true;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        // Once created, the connection recovers by itself, so it is reused even while recovering.
        if (_connection is not null)
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _connection ??= await ConnectAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync(
                    Constants.ReplySuccess,
                    "Service shutting down",
                    CloseTimeout,
                    abort: false);
            }
            catch (Exception ex)
            {
                LogCloseFailed(ex);
            }

            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }

    private async Task<IConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            ClientProvidedName = _clientName,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
        };

        var connection = await _connectPipeline.ExecuteAsync(
            static async (state, token) => await state.CreateConnectionAsync(token),
            factory,
            cancellationToken);

        LogConnected(_options.HostName, _options.Port);
        return connection;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "RabbitMQ is not reachable (attempt {Attempt}); retrying in {Delay}")]
    private partial void LogConnectRetry(Exception? exception, int attempt, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to RabbitMQ at {Host}:{Port}")]
    private partial void LogConnected(string host, int port);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Closing the RabbitMQ connection failed")]
    private partial void LogCloseFailed(Exception exception);
}
