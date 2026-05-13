#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/ops/lib/repo-guard.sh
source "$SCRIPT_DIR/lib/repo-guard.sh"

usage() {
  cat <<'USAGE'
Usage: bash scripts/ops/local-worker-auto-run-once.sh [--lane <lane>] [--limit <count>]

Selects at most one ready CarbonOps-API task and runs the local worker sequence:
claim, prepare prompt, run Codex, and open PR.

Options:
  --lane <lane>      Filter ready task candidates by lane label or Lane metadata.
  --limit <count>    Maximum ready issues to read. Default: 50.
  -h, --help         Show this help.

This wrapper does not run continuously, install a scheduler, process multiple
tasks, merge pull requests, or bypass CI.
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

command -v git >/dev/null 2>&1 || {
  printf 'error: required command not found: git\n' >&2
  exit 1
}

run_status_report() {
  local issue_number="$1"
  local phase="$2"
  local summary="$3"

  bash "$SCRIPT_DIR/local-worker-status-report.sh" \
    --issue "$issue_number" \
    --phase "$phase" \
    --summary "$summary"
}

run_worker_phase() {
  local phase="$1"
  local issue_number="$2"
  shift 2

  printf '\nAuto-run phase: %s\n' "$phase"
  set +e
  bash "$SCRIPT_DIR/local-worker-run-once.sh" "$@" --issue "$issue_number"
  local status="$?"
  set -e

  if [ "$status" -ne 0 ]; then
    printf 'error: auto-run phase %s failed with status %s\n' "$phase" "$status" >&2
    return "$status"
  fi
}

carbonops_api_repo_guard_init
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

  [
    .[]
    | (field("Task ID")) as $task_id
    | (field("Lane")) as $lane
    | [(.labels // [])[]?.name] as $label_names
    | {
        number,
        title,
        url,
        task_id: $task_id,
        lane: (if $lane == "" then "unspecified" else $lane end),
        labels: $label_names,
        task_id_missing: ($task_id == "")
      }
    | select(.task_id_missing | not)
    | select(
        $lane_filter == "" or
        (.lane | ascii_downcase) == ($lane_filter | ascii_downcase) or
        ((.labels | index("lane:" + $lane_filter)) != null)
      )
  ]
  | sort_by((.lane | ascii_downcase), (.task_id | ascii_downcase), .number)
')"

CANDIDATE_COUNT="$(printf '%s\n' "$READY_TASKS_JSON" | jq 'length')"

cat <<SUMMARY
CarbonOps-API local worker auto-run-once

Repository: $CARBONOPS_API_REPOSITORY
Repo root: $CARBONOPS_API_REPO_ROOT
Ready issue limit: $LIMIT
Lane filter: ${LANE_FILTER:-none}
Candidate count: $CANDIDATE_COUNT
Tasks selected for processing: 0
SUMMARY

if [ "$CANDIDATE_COUNT" = "0" ]; then
  printf '\nNo eligible ready task candidate found. Auto-run stopped without mutation.\n'
  exit 0
fi

SELECTED_TASK_JSON="$(printf '%s\n' "$READY_TASKS_JSON" | jq '.[0]')"
SELECTED_ISSUE_NUMBER="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.number')"
SELECTED_TASK_ID="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.task_id')"
SELECTED_TITLE="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.title')"
SELECTED_URL="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.url')"

cat <<SELECTED

Selected task:
Issue: #$SELECTED_ISSUE_NUMBER
Task ID: $SELECTED_TASK_ID
Title: $SELECTED_TITLE
URL: $SELECTED_URL
Tasks selected for processing: 1
SELECTED

CLAIMED="false"
CURRENT_PHASE="claim"

if ! run_worker_phase "$CURRENT_PHASE" "$SELECTED_ISSUE_NUMBER" --claim; then
  exit 1
fi

CLAIMED="true"

CURRENT_PHASE="prepare-prompt"
if ! run_worker_phase "$CURRENT_PHASE" "$SELECTED_ISSUE_NUMBER" --prepare-prompt; then
  run_status_report "$SELECTED_ISSUE_NUMBER" "$CURRENT_PHASE" "Auto-run failed while preparing the prompt artifact."
  exit 1
fi

CURRENT_PHASE="run-codex"
if ! run_worker_phase "$CURRENT_PHASE" "$SELECTED_ISSUE_NUMBER" --run-codex; then
  run_status_report "$SELECTED_ISSUE_NUMBER" "$CURRENT_PHASE" "Auto-run failed while running local Codex."
  exit 1
fi

CURRENT_PHASE="open-pr"
if ! run_worker_phase "$CURRENT_PHASE" "$SELECTED_ISSUE_NUMBER" --open-pr; then
  run_status_report "$SELECTED_ISSUE_NUMBER" "$CURRENT_PHASE" "Auto-run failed while opening the pull request."
  exit 1
fi

cat <<DONE

Local worker auto-run-once completed.

Issue: #$SELECTED_ISSUE_NUMBER
Task ID: $SELECTED_TASK_ID
Claimed: $CLAIMED
Tasks processed: 1
Final phase: open-pr
DONE
