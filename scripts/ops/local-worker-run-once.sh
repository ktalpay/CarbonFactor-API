#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/ops/lib/repo-guard.sh
source "$SCRIPT_DIR/lib/repo-guard.sh"
# shellcheck source=scripts/ops/lib/worker-lock.sh
source "$SCRIPT_DIR/lib/worker-lock.sh"

usage() {
  cat <<'USAGE'
Usage: bash scripts/ops/local-worker-run-once.sh [--claim] [--issue <number>] [--lane <lane>] [--limit <count>]

Local worker for CarbonOps-API.

Default mode is dry-run only. Claim mode must be requested explicitly with
--claim.

Options:
  --claim          Claim one eligible ready task by moving it to status:in-progress.
  --issue <number> Filter candidates to one issue number. Recommended for claim validation.
  --lane <lane>   Filter ready task candidates by lane label or Lane metadata.
  --limit <count> Maximum ready issues to read from GitHub. Default: 50.
  -h, --help      Show this help.

Dry-run mode does not mutate GitHub state, create branches, run Codex, download
artifacts, commit, push, or open pull requests.

Claim mode only updates one issue's status label and writes a claim comment. It
does not run Codex, download artifacts, create branches, commit, push, or open
pull requests.
USAGE
}

CLAIM_MODE="false"
ISSUE_FILTER=""
LANE_FILTER=""
LIMIT="50"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --claim)
      CLAIM_MODE="true"
      shift
      ;;
    --issue)
      if [ "$#" -lt 2 ]; then
        printf 'error: --issue requires a value\n' >&2
        exit 1
      fi
      ISSUE_FILTER="$2"
      shift 2
      ;;
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

if [ -n "$ISSUE_FILTER" ]; then
  case "$ISSUE_FILTER" in
    ''|*[!0-9]*)
      printf 'error: --issue must be a positive integer\n' >&2
      exit 1
      ;;
  esac

  if [ "$ISSUE_FILTER" -lt 1 ]; then
    printf 'error: --issue must be greater than zero\n' >&2
    exit 1
  fi
fi

command -v gh >/dev/null 2>&1 || {
  printf 'error: required command not found: gh\n' >&2
  exit 1
}

command -v jq >/dev/null 2>&1 || {
  printf 'error: required command not found: jq\n' >&2
  exit 1
}

extract_task_id_from_body() {
  awk '
    {
      line = $0
      sub(/^[[:space:]]+/, "", line)
      needle = "task id:"

      if (tolower(substr(line, 1, length(needle))) == needle) {
        value = substr(line, length(needle) + 1)
        sub(/^[[:space:]]+/, "", value)
        sub(/[[:space:]]+$/, "", value)
        print value
        exit
      }
    }
  '
}

carbonops_api_repo_guard_init
carbonops_api_worker_lock_acquire

cd "$CARBONOPS_API_REPO_ROOT"

if [ -n "$ISSUE_FILTER" ]; then
  ISSUE_JSON="$(gh issue view "$ISSUE_FILTER" \
    --repo "$CARBONOPS_API_REPOSITORY" \
    --json number,title,body,url,labels,state)"

  READY_ISSUES_JSON="$(printf '%s\n' "$ISSUE_JSON" | jq '[
    select(.state == "OPEN")
    | select(any((.labels // [])[]?.name; . == "status:ready"))
  ]')"
else
  READY_ISSUES_JSON="$(gh issue list \
    --repo "$CARBONOPS_API_REPOSITORY" \
    --search 'label:"status:ready" state:open' \
    --limit "$LIMIT" \
    --json number,title,body,url,labels)"
fi

READY_TASKS_JSON="$(printf '%s\n' "$READY_ISSUES_JSON" | jq --arg lane_filter "$LANE_FILTER" '
  def field($name):
    [(.body // "" | split("\n")[] | (try capture("^\\s*" + $name + "\\s*:\\s*(?<value>.*)\\s*$"; "i").value catch empty))]
    | first
    | . // "";

  def display:
    if . == "" then "none" else . end;

  [
    .[]
    | (field("Task ID")) as $task_id
    | (field("Lane")) as $lane
    | (field("Agent")) as $agent
    | (field("Depends on")) as $depends_on
    | (field("Unblocks")) as $unblocks
    | [(.labels // [])[]?.name] as $label_names
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
        labels: $label_names
      }
    | select(
        $lane_filter == "" or
        (.lane | ascii_downcase) == ($lane_filter | ascii_downcase) or
        ((.labels | index("lane:" + $lane_filter)) != null)
      )
  ]
  | sort_by((.lane | ascii_downcase), (.task_ref | ascii_downcase), .number)
')"

READY_COUNT="$(printf '%s\n' "$READY_TASKS_JSON" | jq 'length')"
MALFORMED_COUNT="$(printf '%s\n' "$READY_TASKS_JSON" | jq '[.[] | select(.task_id_missing)] | length')"

if [ "$CLAIM_MODE" = "true" ]; then
  MUTATION_MODE="claim"
else
  MUTATION_MODE="disabled"
fi

cat <<SUMMARY
CarbonOps-API local worker run-once

Repository: $CARBONOPS_API_REPOSITORY
Repo root: $CARBONOPS_API_REPO_ROOT
Ready issue limit: $LIMIT
Issue filter: ${ISSUE_FILTER:-none}
Lane filter: ${LANE_FILTER:-none}
Ready candidate count: $READY_COUNT
Malformed candidate count: $MALFORMED_COUNT
Mutation mode: $MUTATION_MODE
Codex invoked: no
Branches created: 0
Commits created: 0
Pull requests opened: 0
Artifacts downloaded: 0
SUMMARY

if [ "$READY_COUNT" = "0" ]; then
  printf '\nNo ready task candidates found.\n'

  if [ "$CLAIM_MODE" = "true" ]; then
    printf 'error: claim requested but no eligible ready task candidate was found\n' >&2
    exit 1
  fi

  exit 0
fi

printf '\nReady task candidates by lane:\n\n'

printf '%s\n' "$READY_TASKS_JSON" | jq -r '.[].lane' | sort -f -u | while IFS= read -r LANE; do
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

if [ "$CLAIM_MODE" != "true" ]; then
  printf '\nDry-run completed without GitHub mutations.\n'
  exit 0
fi

SELECTED_TASK_JSON="$(printf '%s\n' "$READY_TASKS_JSON" | jq '.[0]')"
SELECTED_ISSUE_NUMBER="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.number')"
SELECTED_TASK_ID="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.task_id')"
SELECTED_TASK_ID_MISSING="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.task_id_missing')"
SELECTED_TITLE="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.title')"

if [ "$SELECTED_TASK_ID_MISSING" = "true" ] || [ -z "$SELECTED_TASK_ID" ]; then
  printf 'error: refusing to claim issue #%s because Task ID metadata is missing\n' "$SELECTED_ISSUE_NUMBER" >&2
  exit 1
fi

CURRENT_ISSUE_JSON="$(gh issue view "$SELECTED_ISSUE_NUMBER" \
  --repo "$CARBONOPS_API_REPOSITORY" \
  --json number,title,body,url,labels,state)"

CURRENT_STATE="$(printf '%s\n' "$CURRENT_ISSUE_JSON" | jq -r '.state')"
CURRENT_HAS_READY="$(printf '%s\n' "$CURRENT_ISSUE_JSON" | jq -r 'any((.labels // [])[]?.name; . == "status:ready")')"
CURRENT_TASK_ID="$(printf '%s\n' "$CURRENT_ISSUE_JSON" | jq -r '.body // ""' | extract_task_id_from_body)"

if [ "$CURRENT_STATE" != "OPEN" ]; then
  printf 'error: refusing to claim issue #%s because it is not open\n' "$SELECTED_ISSUE_NUMBER" >&2
  exit 1
fi

if [ "$CURRENT_HAS_READY" != "true" ]; then
  printf 'error: refusing to claim issue #%s because it is not currently labeled status:ready\n' "$SELECTED_ISSUE_NUMBER" >&2
  exit 1
fi

if [ -z "$CURRENT_TASK_ID" ]; then
  printf 'error: refusing to claim issue #%s because current Task ID metadata is missing\n' "$SELECTED_ISSUE_NUMBER" >&2
  exit 1
fi

CLAIM_COMMENT="$(cat <<COMMENT
Local worker claimed this task and moved it to \`status:in-progress\`.

Task ID: \`$CURRENT_TASK_ID\`
Worker phase: claim-only
Next expected phase: prompt artifact lookup/download in a later task

No Codex execution, branch creation, commit, push, artifact download, or pull request creation was performed by this claim step.
COMMENT
)"

gh issue edit "$SELECTED_ISSUE_NUMBER" \
  --repo "$CARBONOPS_API_REPOSITORY" \
  --remove-label "status:ready" \
  --add-label "status:in-progress"

gh issue comment "$SELECTED_ISSUE_NUMBER" \
  --repo "$CARBONOPS_API_REPOSITORY" \
  --body "$CLAIM_COMMENT"

cat <<CLAIMED

Claim completed.

Claimed issue: #$SELECTED_ISSUE_NUMBER
Task ID: $CURRENT_TASK_ID
Title: $SELECTED_TITLE
New status label: status:in-progress
Codex invoked: no
Branches created: 0
Commits created: 0
Pull requests opened: 0
Artifacts downloaded: 0
CLAIMED
