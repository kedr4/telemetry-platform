# WeakApp — behaviour analysis

WeakApp (<https://github.com/nantonov/WeakApp>) ships only as a compiled .NET 9 binary, so its
behaviour was established empirically (≈80 paced requests against the container).

## Endpoints

| Endpoint | Auth | Response |
|---|---|---|
| `GET /health`, `/healthz`, `/.well-known/health` | none | `200 OK` (text/plain) |
| `GET /meters` | `X-Api-Key: supersecret` | see below; `401 Invalid or missing API key` without the header |

## `GET /meters` outcomes

| Outcome | Share | Body |
|---|---|---|
| `200` valid batch | ~60 % | JSON array of 18 readings |
| `200` corrupted | ~13 % | `{"error": "data corrupted"}` |
| `200` empty | ~7 % | `[]` |
| `500` | ~17 % | `Unknown error` |
| `502` | ~13 % | `Bad Gateway` |
| `504` | ~5 % | `Gateway Timeout` |
| hang | rare | no response within 20 s |
| `429` | when > ~10 requests / 30 s | `Too Many Requests: rate limit exceeded`, `Retry-After: <seconds>` |

Normal latency is 0.2–2 s.

## Payload

```json
[
  { "type": "energy",      "name": "Kitchen", "payload": { "energy": 604.88 } },
  { "type": "air_quality", "name": "Garage",  "payload": { "co2": 543, "pm25": 26, "humidity": 68 } },
  { "type": "motion",      "name": "Office",  "payload": { "motionDetected": true } }
]
```

- 6 locations (`name`): Garage, Living Room, Kitchen, Bedroom, Office, Corridor.
- 3 meter types: `energy`, `air_quality`, `motion` → 18 meters per batch.
- Observed ranges: energy 8–990, co2 300–996, pm25 8–48, humidity 22–79.
- **No timestamps and no identifiers** — the ingestor stamps each batch with a `batchId` and `fetchedAt`.

## Consequences for the design

- Per-attempt timeout (the API can hang) plus an overall timeout.
- Retries with exponential backoff and jitter for `5xx`, timeouts and "corrupted" bodies.
- `429` honours `Retry-After`; the poll interval stays below the rate limit.
- Circuit breaker to stop hammering the API during failure streaks.
- Empty and corrupted batches are logged and counted, never published.
- Payload validation per meter type; invalid readings are dropped individually.
- Idempotent persistence keyed on `(batchId, location, type)`.
