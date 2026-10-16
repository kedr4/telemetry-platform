# Frontend — Smart Home Telemetry dashboard

Angular 22 single-page app (standalone components, signals, zoneless change detection) with Angular
Material 3, Apollo Client for GraphQL, `@microsoft/signalr` for live updates and Apache ECharts for charts.

## Pages

| Route        | Content                                                                                                                                                                                                                             |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/` Overview | Latest values per room (updated live over SignalR, with CO₂/PM2.5 comfort levels), trends of a metric per room, rooms compared (average and peak), readings by meter type, live feed of stored batches. Time range 15 min – 7 days. |
| `/readings`  | Stored readings with filters (rooms, meter types, time range), newest first, cursor pagination.                                                                                                                                     |

Every data panel shows a spinner on first load, keeps its content while refreshing (thin progress bar),
and shows the error with a **Retry** button when a request fails. The toolbar shows whether live updates
are flowing; the connection reconnects by itself with exponential back-off. Light and dark themes follow
the operating system.

## Architecture

```
src/app/
  core/
    graphql/     operations (*.graphql), Apollo setup, generated types (do not edit)
    realtime/    SignalR hub connection (status signal, de-duplicated notification stream)
    telemetry/   typed API service, domain helpers (metrics, units, comfort levels, time ranges)
  shared/ui/     panel (loading/error/refresh states), ECharts wrapper, connection status
  features/
    dashboard/   live readings store, room cards, chart panels, live feed
    readings/    explorer with filters and cursor paging
```

- **Data loading** uses Angular resources (`rxResource`) over a typed `TelemetryApi`.
- **Live data:** room cards are patched directly from SignalR notifications (no refetch); aggregation
  panels re-query at most every 15 s while notifications arrive.
- **Same origin:** the app calls `/graphql` and `/hubs/telemetry` on its own origin. `ng serve` proxies
  them to the local services (`proxy.conf.json`); in Docker, nginx does (`nginx.conf`). No CORS involved.

## GraphQL types

Operations live next to the features that use them (`*.graphql`). Types and typed document nodes are
generated from the gateway's committed schema
(`backend/src/Services/Gateway/Telemetry.Gateway.Api/schema.graphql`):

```bash
npm run codegen
```

Commit the regenerated `src/app/core/graphql/generated.ts`.

## Development

```bash
npm ci
npm start                # http://localhost:4200, expects the backend from docker compose
npm test                 # Vitest (watch); npm run test:ci for a single run
npm run lint             # ESLint incl. Angular template accessibility rules
npm run format:check     # Prettier
npm run build            # production build into dist/
```

## Notes

- **Bundle budget:** the initial bundle is ~610 kB raw / ~150 kB transferred — Angular, Apollo Client
  and SignalR, all needed at start-up (ECharts is loaded lazily with the dashboard). The budget warns
  above 650 kB so growth is noticed.
- **`npm audit`:** reports a high-severity `braces` advisory (GHSA-vfj7-8cjw-p6xm, no patched version
  exists) that only reaches the GraphQL code generator, a development tool run on our own files.
  Production dependencies have no known vulnerabilities (`npm audit --omit=dev`).
