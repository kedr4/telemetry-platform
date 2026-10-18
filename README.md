# Smart-home telemetry platform

A microservices system that reliably ingests data from an intentionally unstable external API
([WeakApp](https://github.com/nantonov/WeakApp)), distributes it through RabbitMQ, stores and aggregates it in
PostgreSQL, and presents it in an Angular dashboard over GraphQL, REST and SignalR.

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
| **Grafana** (provisioned dashboards) | <http://localhost:3000> (viewing needs no login; admin / admin to edit) |
| Prometheus | <http://localhost:9090> · alerts: `/alerts` |

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

## Observability

Every service exposes Prometheus metrics on `/metrics` (OpenTelemetry exporter, wired up once in the shared
service defaults). Besides the .NET runtime, ASP.NET Core, SignalR, HttpClient, Polly and Npgsql meters, the
services record domain metrics:

| Meter | Instruments |
|---|---|
| `Telemetry.Ingestor` | `telemetry.ingestor.cycles` (by outcome), `…readings.published`, `…readings.rejected` |
| `Telemetry.Processor` | `telemetry.processor.batches` (stored / duplicate), `…readings.stored`, `…batch.duration` |
| `Telemetry.Notifications` | `telemetry.notifications.clients.connected`, `…sent`, `…readings` |
| `Telemetry.Gateway` | `telemetry.gateway.query.duration` (by query and outcome; GraphQL and REST alike) |
| `Telemetry.Messaging` | `telemetry.messaging.published` / `…consumed` (acked, dead-lettered, requeued), `…handler.retries`, `…handler.duration` |

Prometheus also scrapes RabbitMQ (per-queue depth via the bundled `rabbitmq_prometheus` plugin) and PostgreSQL
(`postgres-exporter`). Grafana starts with two provisioned dashboards:

- **Telemetry overview** (home): pipeline health at a glance, ingestion outcomes, WeakApp status codes and
  latency, Polly retries/timeouts/circuit breaker/rate limiting, the reading flow through every stage, queue
  depths, dead-lettering, and gateway traffic, latency and query outcomes.
- **Service runtime**: CPU, memory, GC, thread pool, exceptions, HTTP, connections, messaging and database
  metrics per service.

Alert rules ([infra/prometheus/alerts.yml](infra/prometheus/alerts.yml), unit-tested with `promtool test rules`):
a service stops being scraped, ingestion publishes nothing for 5 minutes, messages land in a dead-letter queue,
a queue backs up, or more than 5% of gateway requests fail.

## Continuous integration

GitHub Actions runs one workflow per deployable component, each triggered only by changes to its own
code or to the shared code and build settings it depends on.

| Workflow | Runs on changes to | Steps |
|---|---|---|
| `ingestor.yml`, `processor.yml`, `gateway.yml`, `notifications.yml` | the service, its tests, building blocks, shared build settings | format check → Release build (warnings are errors) → unit tests → integration tests (Testcontainers) → Docker image |
| `building-blocks.yml` | shared libraries and test helpers | format check → build → unit and integration tests |
| `frontend.yml` | `frontend/`, the gateway's `schema.graphql` | `npm audit` (production deps) → generated GraphQL types up to date → Prettier → ESLint → Vitest → production build → Docker image |
| `observability.yml` | `infra/` | `promtool check config`, alert rule unit tests, dashboard JSON checks |
| `compose.yml` | `docker-compose.yml`, `.env.example`, `deploy/` | `docker compose config` with defaults, `.env.example` and every deploy environment; ShellCheck; deployment dry run |
| `e2e.yml` | everything except docs; also nightly | `docker compose up --build --wait` → smoke test → Playwright suite (report, traces and service logs attached) |
| `deploy.yml` | a successful image workflow on `main`, or manual dispatch | deploy to `staging` (automatic) or `production` (manual), smoke test, automatic rollback — see [Deployment](#deployment) |

The .NET services share `_dotnet-service.yml`, a reusable workflow parameterised by the service's projects
and Dockerfile. Test results (TRX), Cobertura coverage and a Markdown summary are attached to every run.
Images are built on every run and pushed to `ghcr.io/<owner>/telemetry-<service>` (tagged `sha-<commit>`
and `latest`) only from `main`. Dependabot proposes weekly, grouped updates for NuGet, npm, Docker base
images and the actions themselves.

## End-to-end tests

[e2e/](e2e/) holds a Playwright suite that runs against a running platform (32 tests):

- **API**: REST polymorphism, cursor paging without overlap, filters, problem details; GraphQL fragments,
  aggregations and per-field validation errors.
- **Browser** (desktop Chrome and a Pixel 7 viewport): live status, a card per room, a batch arriving over
  SignalR, charts, time-range switching, navigation, paging and filtering of readings, and axe checks for
  serious WCAG 2.1 AA violations.
- **Monitoring**: every Prometheus target up, readings visible in the pipeline metrics, alert rules loaded,
  Grafana dashboards provisioned.

```bash
docker compose up --build --wait
cd e2e && npm ci && npx playwright install chromium
npm test                       # E2E_APP_URL, E2E_GATEWAY_URL, … point it at another environment
```

## Deployment

`deploy/deploy.sh <environment> [version]` deploys the published images of one version (`latest` or
`sha-<commit>`) and is what the `deploy.yml` workflow runs:

1. pulls `ghcr.io/<owner>/telemetry-*:<version>` (overlay [deploy/compose.release.yml](deploy/compose.release.yml)),
2. starts the environment as its own compose project, with its own network, volumes and ports
   ([deploy/environments/](deploy/environments/)), and waits until every container is healthy,
3. runs [deploy/smoke-test.sh](deploy/smoke-test.sh): readiness of every service, ingested data, GraphQL
   directly and through the frontend proxy, SignalR negotiation,
4. records the version — or, if anything failed, redeploys the last good version and exits non-zero.

The script deploys to whatever Docker engine `DOCKER_HOST` points at. In GitHub Actions that is the
runner itself, so the deployment is a full rehearsal that disappears with the runner (a mock); with
`DOCKER_HOST=ssh://deploy@server` the same script deploys to a real host. `DEPLOY_DRY_RUN=1` prints the
commands. Passwords and keys come from GitHub environment secrets, never from the environment files.

## Repository layout

| Path | Contents |
|---|---|
| `backend/src/BuildingBlocks/` | Message contracts and shared infrastructure (logging, metrics, correlation, health, RabbitMQ) |
| `backend/src/Services/` | One folder per microservice |
| `backend/tests/` | Unit and integration tests, mirroring `src/` |
| `frontend/` | Angular 22 dashboard (Material, Apollo GraphQL, SignalR, ECharts) — see [frontend/README.md](frontend/README.md) |
| `infra/` | Prometheus scrape config and tested alert rules; Grafana provisioning and dashboards |
| `e2e/` | Playwright end-to-end tests of the running platform |
| `deploy/` | Deployment script, smoke test, release overlay and per-environment settings |
| `.github/` | CI/CD workflows and Dependabot configuration |
| `docs/` | Architecture notes, implementation plan, API analysis |
| `docker-compose.yml` | Runs the entire platform |

## Documentation

- [Implementation plan](docs/implementation-plan.md)
- [WeakApp behaviour analysis](docs/weakapp-analysis.md)
