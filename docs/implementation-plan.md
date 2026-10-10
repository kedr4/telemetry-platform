# Implementation plan

Source requirements: `D3S1.pdf`. API behaviour: [weakapp-analysis.md](weakapp-analysis.md).

## Decisions

| Topic | Decision |
|---|---|
| Backend | .NET 10 for every microservice |
| Message queue | RabbitMQ 4.1 |
| Database | PostgreSQL 17, schema owned by the Processor (EF Core migrations) |
| GraphQL | HotChocolate (Nitro playground) in the Gateway |
| REST | Controllers + OpenAPI/Swagger in the Gateway, sharing CQRS handlers with GraphQL |
| Real time | SignalR hub in the Notification service |
| Frontend | Angular (standalone + signals), Apollo Angular, Angular Material, ngx-echarts, `@microsoft/signalr` |
| Resilience | Polly v8 resilience pipeline in the Ingestor |
| Validation | FluentValidation |
| Logging | Serilog (structured JSON, correlation id) |
| Metrics | OpenTelemetry → Prometheus → Grafana |
| Tests | xUnit, NSubstitute, Shouldly, WireMock.Net, Testcontainers; Vitest for Angular; Playwright E2E |
| Quality | Warnings as errors, StyleCop, SonarAnalyzer, `dotnet format`, ESLint, Prettier |
| Git | `main` + `feature/*`, each batch merged with `--no-ff` |

## Architecture

```
WeakApp ──HTTP──▶ Ingestor ──▶ RabbitMQ ──▶ Processor ──▶ PostgreSQL ◀── Gateway (GraphQL + REST) ◀── Angular SPA
                                  ▲   │                                                                  ▲
                                  │   └──▶ Notifications ──SignalR───────────────────────────────────────┘
                                  └── ReadingsPersisted event (published by Processor)
```

| Service | Responsibility |
|---|---|
| Ingestor | Polls `/meters`, survives failures (timeouts, retries, `Retry-After`, circuit breaker), validates, stamps `batchId`/`fetchedAt`, publishes `MeterBatchFetched`. |
| Processor | Consumes batches, upserts meters, inserts readings idempotently, dead-letters poison messages, publishes `ReadingsPersisted`. Applies migrations. |
| Gateway | Clean Architecture (Domain / Application / Infrastructure / Api). CQRS queries via MediatR. GraphQL with filtering, cursor pagination and aggregations; equivalent REST endpoints. |
| Notifications | Consumes `ReadingsPersisted` and broadcasts to SignalR clients (optionally per location group). |
| Frontend | Dashboard (latest values, charts, aggregations by location/type, live feed), readings explorer (filters, paging, time range). |

### Data model

- `meters(id, location, type)` — unique `(location, type)`.
- `readings(id, meter_id, batch_id, recorded_at, energy_kwh, co2_ppm, pm25, humidity, motion_detected)` —
  typed nullable columns with per-type check constraints; unique `(batch_id, meter_id)`;
  indexes on `(meter_id, recorded_at)` and `recorded_at`.

### GraphQL (outline)

- `interface Reading { id, meter, recordedAt }` → `EnergyReading`, `AirQualityReading`, `MotionReading`.
- `meters`, `latestReadings`, `readings(filter, order, first/after)`,
  `aggregates(groupBy: LOCATION | TYPE | TIME, bucket, metric, filter)`, `timeSeries(metric, bucket, filter)`.

### REST (outline)

`/api/v1/meters`, `/api/v1/readings`, `/api/v1/readings/latest`, `/api/v1/aggregates`.

## Repository layout

```
backend/
  src/
    BuildingBlocks/        shared contracts and cross-cutting infrastructure
    Services/<Service>/    one folder per microservice (projects + Dockerfile)
  tests/
    BuildingBlocks/
    Services/<Service>/    unit and integration test projects per service
frontend/                  Angular application
infra/                     Prometheus, Grafana and other infrastructure configuration
e2e/                       Playwright end-to-end tests
deploy/                    deployment scripts
docs/                      documentation
.github/workflows/         one pipeline per service + frontend, E2E, deploy
```

## Batches

| # | Branch | Scope |
|---|---|---|
| 1 | `feature/bootstrap` | Repository layout, build configuration, compose with WeakApp/RabbitMQ/PostgreSQL, docs |
| 2 | `feature/building-blocks` | Message contracts, Serilog, OpenTelemetry, health checks, RabbitMQ helpers, tests |
| 3 | `feature/ingestor` | Ingestor service, Polly pipeline, validation, unit + WireMock integration tests, Dockerfile |
| 4 | `feature/processor` | EF Core schema/migrations, consumer, idempotency, DLQ, event publishing, Testcontainers tests |
| 5 | `feature/gateway` | Clean Architecture gateway, CQRS queries, GraphQL + REST + Swagger, tests |
| 6 | `feature/notifications` | SignalR hub, RabbitMQ consumer, tests |
| 7 | `feature/frontend` | Angular dashboard, Apollo, SignalR, charts, error/loading states, tests, nginx image |
| 8 | `feature/ci` | GitHub Actions per service and frontend: lint, build, test, Docker image |
| 9 | `feature/observability` | OpenTelemetry exporters, Prometheus, provisioned Grafana dashboards |
| 10 | `feature/e2e-and-deploy` | Playwright suite and workflow, mock auto-deploy script/workflow, final docs |

Every batch ends with: build green (no warnings), tests green, stack starts with `docker compose up`.
