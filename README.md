# Smart-home telemetry platform

A microservices system that reliably ingests data from an intentionally unstable external API
([WeakApp](https://github.com/nantonov/WeakApp)), distributes it through RabbitMQ, stores and aggregates it in
PostgreSQL, and presents it in an Angular dashboard over GraphQL, REST and SignalR.

> Work in progress — see [docs/implementation-plan.md](docs/implementation-plan.md) for the roadmap.

## Quick start

Requirements: Docker Desktop (Compose v2).

```bash
docker compose up --build -d
```

| Component | URL |
|---|---|
| WeakApp (unstable API) | <http://localhost:5080/health> |
| Ingestor status | <http://localhost:5101/api/status> · health: `/health/ready` |
| Processor health | <http://localhost:5102/health/ready> |
| RabbitMQ management | <http://localhost:15672> (telemetry / telemetry) |
| PostgreSQL | `localhost:5432` (telemetry / telemetry) |

Ports and credentials can be overridden through a `.env` file — see [.env.example](.env.example).

## Services

| Service | Responsibility |
|---|---|
| **Ingestor** | Polls WeakApp every 6 s through a Polly pipeline (timeouts, jittered retries, circuit breaker, client-side rate limit, truncated-body detection), validates readings with FluentValidation, and publishes `MeterBatchFetched` batches. |
| **Processor** | Consumes batches and stores them in PostgreSQL exactly once (the batch id is the idempotency key), dead-letters malformed batches, and announces stored data with `ReadingsPersisted`. Owns the database schema (EF Core migrations, applied at start-up). |

### Messaging

| Message | Publisher → consumer | Kind |
|---|---|---|
| `MeterBatchFetched` (`meters.batch-fetched`) | Ingestor → Processor (`processor.meter-batches`) | Work item: publishing fails if no queue is bound |
| `ReadingsPersisted` (`readings.persisted`) | Processor → Notifications | Broadcast event: may have no subscribers |

Every consumer queue has a dead-letter queue (`<queue>.dlq`) for messages that cannot be processed.

### Database schema

Owned by the Processor; changes go through EF Core migrations:

```bash
cd backend
dotnet tool restore
dotnet ef migrations add <Name> --project src/Services/Processor/Telemetry.Processor --output-dir Persistence/Migrations
```

## Development

Requirements: .NET 10 SDK, Docker (integration tests start containers through Testcontainers).

```bash
cd backend
dotnet build Telemetry.slnx                              # warnings are errors
dotnet test --solution Telemetry.slnx                    # unit + integration tests
dotnet format Telemetry.slnx --verify-no-changes         # style check
```

## Repository layout

| Path | Contents |
|---|---|
| `backend/src/BuildingBlocks/` | Message contracts and shared infrastructure (logging, correlation, health, RabbitMQ) |
| `backend/src/Services/` | One folder per microservice |
| `backend/tests/` | Unit and integration tests, mirroring `src/` |
| `frontend/` | Angular single-page application |
| `infra/` | Infrastructure configuration (Prometheus, Grafana, …) |
| `docs/` | Architecture notes, implementation plan, API analysis |
| `docker-compose.yml` | Runs the entire platform |

## Documentation

- [Implementation plan](docs/implementation-plan.md)
- [WeakApp behaviour analysis](docs/weakapp-analysis.md)
