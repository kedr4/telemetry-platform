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
| RabbitMQ management | <http://localhost:15672> (telemetry / telemetry) |
| PostgreSQL | `localhost:5432` (telemetry / telemetry) |

Ports and credentials can be overridden through a `.env` file — see [.env.example](.env.example).

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
