#!/usr/bin/env bash
# Post-deployment smoke test: every service is ready, the app and its proxies answer, and real data
# comes back through GraphQL, REST and the SignalR negotiation endpoint.
#
# Usage: deploy/smoke-test.sh <environment-file>
#   SMOKE_HOST     host the environment's ports are published on (default: localhost)
#   SMOKE_TIMEOUT  seconds to wait for readiness and the first ingested data (default: 180)
set -euo pipefail

env_file=${1:?usage: smoke-test.sh <environment-file>}
host=${SMOKE_HOST:-localhost}
timeout=${SMOKE_TIMEOUT:-180}

port() {
  local value
  value=$(grep -E "^$1=" "$env_file" | tail -n 1 | cut -d= -f2-)
  echo "${value:-$2}"
}

app="http://$host:$(port FRONTEND_PORT 4200)"
gateway="http://$host:$(port GATEWAY_PORT 5103)"
notifications="http://$host:$(port NOTIFICATIONS_PORT 5104)"
failures=0

pass() { echo "  ✔ $1"; }
fail() { echo "  ✘ $1" >&2; failures=$((failures + 1)); }

# Retries a check until it succeeds or the deadline passes.
eventually() {
  local description=$1
  shift
  local deadline=$((SECONDS + timeout))
  until "$@" > /dev/null 2>&1; do
    if ((SECONDS >= deadline)); then
      fail "$description (gave up after ${timeout}s)"
      return 0
    fi
    sleep 3
  done
  pass "$description"
}

http_ok() { curl --fail --silent --max-time 5 --output /dev/null "$1"; }

has_locations() {
  curl --fail --silent --max-time 5 "$gateway/api/v1/locations" | grep -q '"'
}

graphql_ok() {
  local body
  body=$(curl --fail --silent --max-time 5 -H 'Content-Type: application/json' \
    -d '{"query":"{ latestReadings { __typename location } }"}' "$1/graphql")
  grep -q '"latestReadings":\[{' <<< "$body" && ! grep -q '"errors"' <<< "$body"
}

signalr_ok() {
  curl --fail --silent --max-time 5 -X POST "$1/hubs/telemetry/negotiate?negotiateVersion=1" \
    | grep -q '"connectionToken"'
}

echo "Smoke testing $app"
for service in INGESTOR PROCESSOR GATEWAY NOTIFICATIONS; do
  url="http://$host:$(port "${service}_PORT" '')/health/ready"
  eventually "${service,,} is ready" http_ok "$url"
done
eventually "frontend is serving" http_ok "$app/healthz"
eventually "readings have been ingested" has_locations
eventually "GraphQL returns readings" graphql_ok "$gateway"
eventually "GraphQL works through the frontend proxy" graphql_ok "$app"
eventually "SignalR negotiation works through the frontend proxy" signalr_ok "$app"
eventually "SignalR hub accepts connections" signalr_ok "$notifications"

if ((failures > 0)); then
  echo "Smoke test failed: $failures check(s)." >&2
  exit 1
fi
echo "Smoke test passed."
