#!/usr/bin/env bash
# Deploys one version of the published images to an environment, smoke-tests it and rolls back to the
# previously deployed version if the smoke test fails.
#
# Usage: deploy/deploy.sh <environment> [version]
#   environment       name of a file in deploy/environments/ (staging, production)
#   version           image tag to deploy (default: latest), e.g. sha-1a2b3c4
#
# Environment variables:
#   REGISTRY          image registry and namespace (default: ghcr.io/<GITHUB_REPOSITORY_OWNER>)
#   DOCKER_HOST       Docker engine to deploy to; e.g. ssh://deploy@server for a remote host.
#                     Unset, the local engine is used (the mock deployment in CI).
#   DEPLOY_STATE_DIR  where the deployed version per environment is recorded (default: .deploy)
#   DEPLOY_DRY_RUN=1  print the commands instead of running them
set -euo pipefail

root=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
environment=${1:?usage: deploy.sh <environment> [version]}
version=${2:-latest}
env_file="$root/deploy/environments/$environment.env"
state_dir=${DEPLOY_STATE_DIR:-$root/.deploy}
state_file="$state_dir/$environment.version"
owner=${GITHUB_REPOSITORY_OWNER:-}
export REGISTRY=${REGISTRY:-ghcr.io/${owner,,}}
export VERSION=$version

[[ -f $env_file ]] || { echo "Unknown environment '$environment' (no $env_file)." >&2; exit 2; }
[[ $REGISTRY != */ && $REGISTRY != "ghcr.io/" ]] \
  || { echo "Set REGISTRY or GITHUB_REPOSITORY_OWNER." >&2; exit 2; }

log() { echo "[$(date -u +%H:%M:%S)] $*"; }

run() {
  if [[ ${DEPLOY_DRY_RUN:-} == 1 ]]; then
    echo "+ $*"
  else
    "$@"
  fi
}

compose() {
  run docker compose --project-directory "$root" --env-file "$env_file" \
    -f "$root/docker-compose.yml" -f "$root/deploy/compose.release.yml" "$@"
}

release() {
  export VERSION=$1
  log "Pulling $REGISTRY/telemetry-*:$VERSION"
  compose pull ingestor processor gateway notifications frontend
  # WeakApp stands in for the external API and is built from its pinned repository revision.
  compose build weakapp
  log "Starting $environment"
  compose up --detach --no-build --wait --wait-timeout 300 --remove-orphans
}

smoke_test() {
  if [[ ${DEPLOY_DRY_RUN:-} == 1 ]]; then
    echo "+ $root/deploy/smoke-test.sh $env_file"
    return 0
  fi
  "$root/deploy/smoke-test.sh" "$env_file"
}

previous=""
[[ -f $state_file ]] && previous=$(< "$state_file")

log "Deploying version '$version' to $environment (previous: ${previous:-none}) on ${DOCKER_HOST:-the local Docker engine}"

if release "$version" && smoke_test; then
  mkdir -p "$state_dir"
  [[ ${DEPLOY_DRY_RUN:-} == 1 ]] || echo "$version" > "$state_file"
  log "Deployed '$version' to $environment."
  exit 0
fi

log "Deployment of '$version' to $environment failed."
if [[ -n $previous && $previous != "$version" ]]; then
  log "Rolling back to '$previous'."
  if release "$previous" && smoke_test; then
    log "Rolled back to '$previous'."
  else
    log "Rollback to '$previous' failed as well; $environment needs attention."
  fi
else
  log "No earlier version to roll back to."
fi
exit 1
