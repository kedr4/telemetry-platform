using Telemetry.Ingestor.Ingestion;

namespace Telemetry.Ingestor;

public static class IngestorEndpoints
{
    public const string StatusPath = "/api/status";

    public static IEndpointRouteBuilder MapIngestorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(StatusPath, (IngestionStatus status) => TypedResults.Ok(status.Snapshot))
            .WithName("GetIngestionStatus")
            .WithSummary("Recent ingestion activity: last outcome, last success and counters.");

        return endpoints;
    }
}
