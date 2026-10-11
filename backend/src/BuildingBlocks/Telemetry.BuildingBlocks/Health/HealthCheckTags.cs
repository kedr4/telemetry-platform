namespace Telemetry.BuildingBlocks.Health;

public static class HealthCheckTags
{
    /// <summary>Checks with this tag must pass before the service accepts traffic.</summary>
    public const string Ready = "ready";
}
