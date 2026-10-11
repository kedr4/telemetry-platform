using System.ComponentModel.DataAnnotations;

namespace Telemetry.BuildingBlocks.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string HostName { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string VirtualHost { get; set; } = "/";

    /// <summary>Gets or sets the maximum number of unacknowledged messages delivered to a consumer.</summary>
    [Range(1, 1000)]
    public ushort PrefetchCount { get; set; } = 16;

    /// <summary>Gets or sets how many times a failing message handler is retried before the message is dead-lettered.</summary>
    [Range(0, 10)]
    public int ConsumerRetryCount { get; set; } = 3;

    /// <summary>Gets or sets the base delay of the exponential back-off between handler retries.</summary>
    public TimeSpan ConsumerRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
