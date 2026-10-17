# Smart-home telemetry platform

A microservices system that reliably ingests data from an intentionally unstable external API
([WeakApp](https://github.com/nantonov/WeakApp)), distributes it through RabbitMQ, stores and aggregates it in
PostgreSQL, and presents it in an Angular dashboard over GraphQL, REST and SignalR.

> Work in progress — see [docs/implementation-plan.md](docs/implementation-plan.md) for the roadmap.

`docker compose up --build -d` starts everything; open <http://localhost:4200> for the live dashboard.

## Quick start

Requirements: Docker Desktop (Compose v2).

```bash
docker compose up --build -d
```

| Component | URL |
|---|---|
| **Dashboard** (Angular app) | <http://localhost:4200> |
| WeakApp (unstable API) | <http://localhost:5080/health> |
| Ingestor status | <http://localhost:5101/api/status> · health: `/health/ready` |
| Processor health | <http://localhost:5102/health/ready> |
| **GraphQL** (Nitro IDE in the browser) | <http://localhost:5103/graphql> |
| **REST** (Swagger UI) | <http://localhost:5103/swagger> · OpenAPI: `/openapi/v1.json` |
| **SignalR hub** | `http://localhost:5104/hubs/telemetry` |
| RabbitMQ management | <http://localhost:15672> (telemetry / telemetry) |
| PostgreSQL | `localhost:5432` (telemetry / telemetry) |

Ports and credentials can be overridden through a `.env` file — see [.env.example](.env.example).

## Services

| Service | Responsibility |
|---|---|
| **Ingestor** | Polls WeakApp every 6 s through a Polly pipeline (timeouts, jittered retries, circuit breaker, client-side rate limit, truncated-body detection), validates readings with FluentValidation, and publishes `MeterBatchFetched` batches. |
| **Processor** | Consumes batches and stores them in PostgreSQL exactly once (the batch id is the idempotency key), dead-letters malformed batches, and announces stored data with `ReadingsPersisted`. Owns the database schema (EF Core migrations, applied at start-up). |
| **Notifications** | Consumes `ReadingsPersisted` and pushes new readings to browsers over SignalR (WebSockets): every client receives each batch, and clients can subscribe to individual locations. |
| **Gateway** | Read side, built with Clean Architecture (Domain → Application → Infrastructure → Api). CQRS queries with MediatR and FluentValidation, served both as a strongly typed GraphQL API (HotChocolate) and as REST (minimal APIs + OpenAPI). Filtering, cursor pagination and aggregations by location, type and time. |

### Gateway API

Both APIs send the same MediatR queries, so they share validation and data access.

| Use case | GraphQL | REST |
|---|---|---|
| Meters / locations | `meters(types)`, `locations` | `GET /api/v1/meters?type=`, `GET /api/v1/locations` |
| Latest value per meter | `latestReadings(filter)` | `GET /api/v1/readings/latest` |
| Readings (newest first, cursor paging) | `readings(filter, first, after)` | `GET /api/v1/readings?first=&after=` |
| Aggregations | `aggregates(input: { groupBy, metric, bucket, filter })` | `GET /api/v1/aggregates?groupBy=&metric=&bucket=` |

Filters: `location`, `type`, `from`, `to`. Aggregations group by any combination of `LOCATION`, `TYPE` and
`TIME` (with a `MINUTE`/`HOUR`/`DAY` bucket) and return count, average, minimum, maximum and sum of a metric
(`ENERGY`, `CO2`, `PM25`, `HUMIDITY`, `MOTION` = share of readings with motion), or only counts without a metric.

Example:

```graphql
{
  aggregates(input: { groupBy: [TIME, LOCATION], metric: CO2, bucket: HOUR }) {
    location bucketStart average maximum
  }
}
```

The schema is committed as [`schema.graphql`](backend/src/Services/Gateway/Telemetry.Gateway.Api/schema.graphql);
a test fails when it drifts (`UPDATE_GRAPHQL_SCHEMA=1 dotnet test` accepts an intended change).

### Real-time notifications

Hub `/hubs/telemetry` (strongly typed, JSON protocol):

| Direction | Method | Payload |
|---|---|---|
| server → every client | `ReadingsUpdated` | `{ batchId, recordedAt, readings: [{ location, type, recordedAt, energy? \| co2?, pm25?, humidity? \| motionDetected? }] }` |
| server → subscribers of a location | `LocationReadingsUpdated` | `(location, notification)` with only that location's readings |
| client → server | `SubscribeToLocation(location)` / `UnsubscribeFromLocation(location)` | — |

Notifications are at-least-once; `batchId` lets clients ignore a repeated batch. The service runs as a single
instance: scaling out would need a SignalR backplane (e.g. Redis) or a queue per instance.

### Messaging

| Message | Publisher → consumer | Kind |
|---|---|---|
| `MeterBatchFetched` (`meters.batch-fetched`) | Ingestor → Processor (`processor.meter-batches`) | Work item: publishing fails if no queue is bound |
| `ReadingsPersisted` (`readings.persisted`) | Processor → Notifications (`notifications.readings-persisted`) | Broadcast event: may have no subscribers |

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

## Continuous integration

GitHub Actions runs one workflow per deployable component, each triggered only by changes to its own
code or to the shared code and build settings it depends on.

| Workflow | Runs on changes to | Steps |
|---|---|---|
| `ingestor.yml`, `processor.yml`, `gateway.yml`, `notifications.yml` | the service, its tests, building blocks, shared build settings | format check → Release build (warnings are errors) → unit tests → integration tests (Testcontainers) → Docker image |
| `building-blocks.yml` | shared libraries and test helpers | format check → build → unit and integration tests |
| `frontend.yml` | `frontend/`, the gateway's `schema.graphql` | `npm audit` (production deps) → generated GraphQL types up to date → Prettier → ESLint → Vitest → production build → Docker image |
| `compose.yml` | `docker-compose.yml`, `.env.example` | `docker compose config` with defaults and with `.env.example` |

The .NET services share `_dotnet-service.yml`, a reusable workflow parameterised by the service's projects
and Dockerfile. Test results (TRX), Cobertura coverage and a Markdown summary are attached to every run.
Images are built on every run and pushed to `ghcr.io/<owner>/telemetry-<service>` (tagged `sha-<commit>`
and `latest`) only from `main`. Dependabot proposes weekly, grouped updates for NuGet, npm, Docker base
images and the actions themselves.

## Repository layout

| Path | Contents |
|---|---|
| `backend/src/BuildingBlocks/` | Message contracts and shared infrastructure (logging, correlation, health, RabbitMQ) |
| `backend/src/Services/` | One folder per microservice |
| `backend/tests/` | Unit and integration tests, mirroring `src/` |
| `frontend/` | Angular 22 dashboard (Material, Apollo GraphQL, SignalR, ECharts) — see [frontend/README.md](frontend/README.md) |
| `infra/` | Infrastructure configuration (Prometheus, Grafana, …) |
| `docs/` | Architecture notes, implementation plan, API analysis |
| `docker-compose.yml` | Runs the entire platform |

## Documentation

- [Implementation plan](docs/implementation-plan.md)
- [WeakApp behaviour analysis](docs/weakapp-analysis.md)
