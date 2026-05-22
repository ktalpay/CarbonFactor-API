#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
api_project="$repo_root/src/dotnet/src/CarbonOps.Api/CarbonOps.Api.csproj"
dsn_env_var="CARBONOPS_POSTGRESQL_TEST_DSN"
port="${CARBONOPS_DOTNET_STARTUP_PORT:-18080}"
base_url="http://127.0.0.1:${port}"
api_pid=""
work_dir=""

usage() {
  cat <<'USAGE'
Usage: scripts/ops/validate-dotnet-postgresql-startup.sh [--check-only]

Validates the .NET API PostgreSQL-backed startup path with startup schema bootstrap.

Required environment:
  CARBONOPS_POSTGRESQL_TEST_DSN   PostgreSQL connection string supplied by the operator.

Optional environment:
  CARBONOPS_DOTNET_STARTUP_PORT   Local test port. Defaults to 18080.

Options:
  --check-only   Validate tools, project path, and required environment without starting the API.

The script does not print the PostgreSQL connection string.
USAGE
}

require_command() {
  local command_name="$1"
  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "$command_name is required for .NET PostgreSQL startup validation." >&2
    return 127
  fi
}

check_prerequisites() {
  require_command dotnet
  require_command curl

  if [ ! -f "$api_project" ]; then
    echo "CarbonOps.Api project was not found at the expected path." >&2
    return 1
  fi

  if [ -z "${CARBONOPS_POSTGRESQL_TEST_DSN:-}" ]; then
    echo "${dsn_env_var} is required but was not set. Provide a PostgreSQL connection string through this environment variable; the value will not be printed." >&2
    return 1
  fi
}

cleanup() {
  if [ -n "$api_pid" ] && kill -0 "$api_pid" >/dev/null 2>&1; then
    kill "$api_pid" >/dev/null 2>&1 || true
    wait "$api_pid" >/dev/null 2>&1 || true
  fi

  if [ -n "$work_dir" ] && [ -d "$work_dir" ]; then
    rm -rf "$work_dir"
  fi
}

wait_for_endpoint() {
  local path="$1"
  local label="$2"
  local attempts=60

  for _ in $(seq 1 "$attempts"); do
    if curl --fail --silent --show-error --max-time 2 "${base_url}${path}" >/dev/null 2>&1; then
      echo "${label} passed."
      return 0
    fi

    if [ -n "$api_pid" ] && ! kill -0 "$api_pid" >/dev/null 2>&1; then
      echo "The .NET API process exited before ${label} passed." >&2
      return 1
    fi

    sleep 1
  done

  echo "Timed out waiting for ${label} at ${base_url}${path}." >&2
  return 1
}

case "${1:-}" in
  "")
    ;;
  "--check-only")
    check_prerequisites
    echo "PostgreSQL startup validation prerequisites are available. No service was started."
    exit 0
    ;;
  "-h" | "--help")
    usage
    exit 0
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac

check_prerequisites

if command -v lsof >/dev/null 2>&1 && lsof -iTCP:"$port" -sTCP:LISTEN -n -P >/dev/null 2>&1; then
  echo "Port ${port} is already in use. Set CARBONOPS_DOTNET_STARTUP_PORT to a free local port." >&2
  exit 1
fi

trap cleanup EXIT INT TERM
work_dir="$(mktemp -d "${TMPDIR:-/tmp}/carbonops-dotnet-pg-startup.XXXXXX")"
api_log="$work_dir/api.log"

(
  cd "$repo_root"
  export ASPNETCORE_ENVIRONMENT=Production
  export ASPNETCORE_URLS="$base_url"
  export Persistence__UsePostgreSql=true
  export Persistence__PostgreSql__ConnectionString="$CARBONOPS_POSTGRESQL_TEST_DSN"
  export Persistence__PostgreSql__BootstrapOnStartup=true
  export Persistence__PostgreSql__BootstrapMode=CreateMissing
  export Security__ApiKey__ImportEndpointKeyHash=cf52b8d448a00cd93a9124b8a291ce6d7910b3065ac82d2d7eb231589b158b46
  export Security__ApiKey__ImportTenantId=tenant-self-hosted-validation
  export Security__ApiKey__ImportEndpointScopes__0=carbon_factors:import
  export RateLimiting__Import__PermitLimit=100
  export RateLimiting__Import__WindowSeconds=60
  export RateLimiting__Import__QueueLimit=0
  export RateLimiting__Read__PermitLimit=100
  export RateLimiting__Read__WindowSeconds=60
  export RateLimiting__Read__QueueLimit=0

  dotnet run --no-launch-profile --project "$api_project" >"$api_log" 2>&1
) &
api_pid="$!"

echo "Started .NET API PostgreSQL startup validation on ${base_url}."
wait_for_endpoint "/health" "GET /health"
wait_for_endpoint "/health/ready" "GET /health/ready"
wait_for_endpoint "/v1/carbon-factors" "GET /v1/carbon-factors"

echo "Validated .NET PostgreSQL startup bootstrap with health, readiness, and v1 read smoke checks."
