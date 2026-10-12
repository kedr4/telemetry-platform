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
| `200` corrupted | ~13 % | `{"error": "data corrupted"}` sent as a chunked body whose stream is then **aborted** (no terminating chunk) |
| `200` empty | ~7 % | `[]` |
| `500` | ~17 % | `Unknown error` |
| `502` | ~13 % | `Bad Gateway` |
| `504` | ~5 % | `Gateway Timeout` |
| hang | rare | no response within 20 s |
| `429` | above **10 requests per sliding 40 s window** | `Too Many Requests: rate limit exceeded`, `Retry-After: <seconds until the oldest request leaves the window>` |

Normal latency is 0.2–2 s.

The "corrupted" case is a transport failure rather than a valid JSON answer: clients see
`200 OK` followed by "response ended prematurely" while reading the body (curl exit code 18).
It only shows up as a normal response when the body is read outside the retry pipeline.

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
- Retries with exponential backoff and jitter for `5xx`, timeouts and truncated ("corrupted") bodies;
  the body is buffered inside each attempt so a truncation fails that attempt.
- A client-side sliding-window limiter (9 requests / 40 s) keeps every HTTP attempt, retries included,
  below the server limit; a `429` that still happens is honoured through `Retry-After`.
- Circuit breaker to stop hammering the API during failure streaks.
- Empty and corrupted batches are logged and counted, never published.
- Payload validation per meter type; invalid readings are dropped individually.
- Idempotent persistence keyed on `(batchId, location, type)`.
