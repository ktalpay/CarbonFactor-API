#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/ops/lib/repo-guard.sh
source "$SCRIPT_DIR/lib/repo-guard.sh"
# shellcheck source=scripts/ops/lib/worker-lock.sh
source "$SCRIPT_DIR/lib/worker-lock.sh"

usage() {
  cat <<'USAGE'
Usage: scripts/ops/local-worker-run-once.sh [--lane <lane>] [--limit <count>]

Dry-run only local worker for CarbonOps-API.

Options:
  --lane <lane>     Filter ready task candidates by lane label or Lane metadata.
  --limit <count>   Maximum ready issues to read from GitHub. Default: 50.
  -h, --help        Show this help.

This script does not mutate GitHub state, create branches, run Codex, download
artifacts, commit, push, or open pull requests.
USAGE
}

LANE_FILTER=""
LIMIT="50"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --lane)
      if [ "$#" -lt 2 ]; then
        printf 'error: --lane requires a value\n' >&2
        exit 1
      fi
      LANE_FILTER="$2"
      shift 2
      ;;
    --limit)
      if [ "$#" -lt 2 ]; then
        printf 'error: --limit requires a value\n' >&2
        exit 1
      fi
      LIMIT="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      printf 'error: unknown argument: %s\n' "$1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

case "$LIMIT" in
  ''|*[!0-9]*)
    printf 'error: --limit must be a positive integer\n' >&2
    exit 1
    ;;
esac

if [ "$LIMIT" -lt 1 ]; then
  printf 'error: --limit must be greater than zero\n' >&2
  exit 1
fi

command -v gh >/dev/null 2>&1 || {
  printf 'error: required command not found: gh\n' >&2
  exit 1
}

command -v jq >/dev/null 2>&1 || {
  printf 'error: required command not found: jq\n' >&2
  exit 1
}

carbonops_api_repo_guard_init
carbonops_api_worker_lock_acquire

cd "$CARBONOPS_API_REPO_ROOT"

READY_ISSUES_JSON="$(gh issue list \
  --repo "$CARBONOPS_API_REPOSITORY" \
  --search 'label:"status:ready" state:open' \
  --limit "$LIMIT" \
  --json number,title,body,url,labels)"

READY_TASKS_JSON="$(printf '%s\n' "$READY_ISSUES_JSON" | jq --arg lane_filter "$LANE_FILTER" '
  def field($name):
    [(.body // "" | split("\n")[] | (try capture("^\\s*" + $name + "\\s*:\\s*(?<value>.*)\\s*$"; "i").value catch empty))]
    | first
    | . // "";

  def display:
    if . == "" then "none" else . end;

  def has_label($name):
    any((.labels // [])[]?.name; . == $name);

  [
    .[]
    | (field("Task ID")) as $task_id
    | (field("Lane")) as $lane
    | (field("Agent")) as $agent
    | (field("Depends on")) as $depends_on
    | (field("Unblocks")) as $unblocks
    | {
        number,
        title,
        url,
        task_id: $task_id,
        task_ref: (if $task_id == "" then .title else $task_id end),
        task_id_missing: ($task_id == ""),
        lane: (if $lane == "" then "unspecified" else $lane end),
        agent: ($agent | display),
        depends_on: ($depends_on | display),
        unblocks: ($unblocks | display),
        labels: [(.labels // [])[]?.name]
      }
    | select(
        $lane_filter == "" or
        (.lane | ascii_downcase) == ($lane_filter | ascii_downcase) or
        has_label("lane:" + $lane_filter)
      )
  ]
  | sort_by((.lane | ascii_downcase), (.task_ref | ascii_downcase), .number)
')"

READY_COUNT="$(printf '%s\n' "$READY_TASKS_JSON" | jq 'length')"
MALFORMED_COUNT="$(printf '%s\n' "$READY_TASKS_JSON" | jq '[.[] | select(.task_id_missing)] | length')"

cat <<SUMMARY
CarbonOps-API local worker dry-run

Repository: $CARBONOPS_API_REPOSITORY
Repo root: $CARBONOPS_API_REPO_ROOT
Ready issue limit: $LIMIT
Lane filter: ${LANE_FILTER:-none}
Ready candidate count: $READY_COUNT
Malformed candidate count: $MALFORMED_COUNT
Mutation mode: disabled
Codex invoked: no
Branches created: 0
Commits created: 0
Pull requests opened: 0
Artifacts downloaded: 0
SUMMARY

if [ "$READY_COUNT" = "0" ]; then
  printf '\nNo ready task candidates found.\n'
  exit 0
fi

printf '\nReady task candidates by lane:\n\n'

mapfile -t LANES < <(printf '%s\n' "$READY_TASKS_JSON" | jq -r '.[].lane' | sort -f -u)

for LANE in "${LANES[@]}"; do
  printf 'Lane: %s\n' "$LANE"
  printf '%s\n' "$READY_TASKS_JSON" | jq -r --arg lane "$LANE" '
    .[]
    | select(.lane == $lane)
    | "- #\(.number) `\(.task_ref)`: \(.title)\n  URL: \(.url)\n  Agent: \(.agent)\n  Depends on: \(.depends_on)\n  Unblocks: \(.unblocks)\n  Labels: \(.labels | join(", "))"
  '
  printf '\n'
done

if [ "$MALFORMED_COUNT" != "0" ]; then
  printf 'Malformed ready candidates missing Task ID:\n'
  printf '%s\n' "$READY_TASKS_JSON" | jq -r '
    .[]
    | select(.task_id_missing)
    | "- #\(.number): \(.title)"
  '
fi

printf '\nDry-run completed without GitHub mutations.\n'
