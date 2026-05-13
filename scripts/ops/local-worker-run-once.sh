#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/ops/lib/repo-guard.sh
source "$SCRIPT_DIR/lib/repo-guard.sh"
# shellcheck source=scripts/ops/lib/worker-lock.sh
source "$SCRIPT_DIR/lib/worker-lock.sh"

usage() {
  cat <<'USAGE'
Usage: bash scripts/ops/local-worker-run-once.sh [--claim|--prepare-prompt|--run-codex] [--issue <number>] [--lane <lane>] [--limit <count>]

Local worker for CarbonOps-API.

Default mode is dry-run only. Mutating/preparation/execution modes must be
requested explicitly.

Options:
  --claim            Claim one eligible ready task by moving it to status:in-progress.
  --prepare-prompt   Locate/download a task prompt artifact, or trigger prompt handoff generation.
  --run-codex        Run local Codex CLI against an already prepared prompt artifact.
  --issue <number>   Filter candidates to one issue number. Recommended for validation.
  --lane <lane>      Filter ready task candidates by lane label or Lane metadata.
  --limit <count>    Maximum ready issues/artifacts to read. Default: 50.
  -h, --help         Show this help.

Dry-run mode does not mutate GitHub state, create branches, run Codex, download
artifacts, commit, push, or open pull requests.

Claim mode only updates one issue's status label and writes a claim comment. It
does not run Codex, download artifacts, create branches, commit, push, or open
pull requests.

Prompt preparation mode may trigger prompt handoff generation or download a
prompt artifact. It does not run Codex, create branches, commit, push, or open
pull requests.

Codex execution mode reads a prepared prompt artifact and captures local Codex
output under .agent-handoff/logs/. It does not create branches, commit, push, or
open pull requests.
USAGE
}

CLAIM_MODE="false"
PREPARE_PROMPT_MODE="false"
RUN_CODEX_MODE="false"
ISSUE_FILTER=""
LANE_FILTER=""
LIMIT="50"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --claim)
      CLAIM_MODE="true"
      shift
      ;;
    --prepare-prompt)
      PREPARE_PROMPT_MODE="true"
      shift
      ;;
    --run-codex)
      RUN_CODEX_MODE="true"
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

REQUESTED_MODES=0
[ "$CLAIM_MODE" = "true" ] && REQUESTED_MODES=$((REQUESTED_MODES + 1))
[ "$PREPARE_PROMPT_MODE" = "true" ] && REQUESTED_MODES=$((REQUESTED_MODES + 1))
[ "$RUN_CODEX_MODE" = "true" ] && REQUESTED_MODES=$((REQUESTED_MODES + 1))

if [ "$REQUESTED_MODES" -gt 1 ]; then
  printf 'error: --claim, --prepare-prompt, and --run-codex are mutually exclusive\n' >&2
  exit 1
fi

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

if [ "$PREPARE_PROMPT_MODE" = "true" ]; then
  command -v unzip >/dev/null 2>&1 || {
    printf 'error: required command not found: unzip\n' >&2
    exit 1
  }
fi

if [ "$RUN_CODEX_MODE" = "true" ]; then
  command -v codex >/dev/null 2>&1 || {
    printf 'error: required command not found: codex\n' >&2
    exit 1
  }
fi

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

find_prepared_prompt_path() {
  local task_id="$1"
  local downloads_root="$CARBONOPS_API_REPO_ROOT/.agent-handoff/downloads"

  if [ ! -d "$downloads_root" ]; then
    return 1
  fi

  find "$downloads_root" \
    -type f \
    -path "*/task-prompt-$task_id-*/*-prompt.md" \
    -print 2>/dev/null |
    sort |
    tail -n 1
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
    | select(any((.labels // [])[]?.name; . == "status:ready" or . == "status:in-progress"))
  ]')"
else
  READY_ISSUES_JSON="$(gh issue list \
    --repo "$CARBONOPS_API_REPOSITORY" \
    --search '(label:"status:ready" OR label:"status:in-progress") state:open' \
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
        labels: $label_names,
        is_ready: (($label_names | index("status:ready")) != null),
        is_in_progress: (($label_names | index("status:in-progress")) != null)
      }
    | select(
        $lane_filter == "" or
        (.lane | ascii_downcase) == ($lane_filter | ascii_downcase) or
        ((.labels | index("lane:" + $lane_filter)) != null)
      )
  ]
  | sort_by((.lane | ascii_downcase), (.task_ref | ascii_downcase), .number)
')"

if [ "$CLAIM_MODE" = "true" ]; then
  CANDIDATE_TASKS_JSON="$(printf '%s\n' "$READY_TASKS_JSON" | jq '[.[] | select(.is_ready)]')"
elif [ "$PREPARE_PROMPT_MODE" = "true" ] || [ "$RUN_CODEX_MODE" = "true" ]; then
  CANDIDATE_TASKS_JSON="$READY_TASKS_JSON"
else
  CANDIDATE_TASKS_JSON="$(printf '%s\n' "$READY_TASKS_JSON" | jq '[.[] | select(.is_ready)]')"
fi

READY_COUNT="$(printf '%s\n' "$CANDIDATE_TASKS_JSON" | jq 'length')"
MALFORMED_COUNT="$(printf '%s\n' "$CANDIDATE_TASKS_JSON" | jq '[.[] | select(.task_id_missing)] | length')"

if [ "$CLAIM_MODE" = "true" ]; then
  MUTATION_MODE="claim"
elif [ "$PREPARE_PROMPT_MODE" = "true" ]; then
  MUTATION_MODE="prompt-prepare"
elif [ "$RUN_CODEX_MODE" = "true" ]; then
  MUTATION_MODE="codex-run"
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
Candidate count: $READY_COUNT
Malformed candidate count: $MALFORMED_COUNT
Mutation mode: $MUTATION_MODE
Branches created: 0
Commits created: 0
Pull requests opened: 0
SUMMARY

if [ "$READY_COUNT" = "0" ]; then
  printf '\nNo eligible task candidates found.\n'

  if [ "$CLAIM_MODE" = "true" ]; then
    printf 'error: claim requested but no eligible ready task candidate was found\n' >&2
    exit 1
  fi

  if [ "$PREPARE_PROMPT_MODE" = "true" ]; then
    printf 'error: prompt preparation requested but no eligible task candidate was found\n' >&2
    exit 1
  fi

  if [ "$RUN_CODEX_MODE" = "true" ]; then
    printf 'error: Codex run requested but no eligible task candidate was found\n' >&2
    exit 1
  fi

  exit 0
fi

printf '\nTask candidates by lane:\n\n'

printf '%s\n' "$CANDIDATE_TASKS_JSON" | jq -r '.[].lane' | sort -f -u | while IFS= read -r LANE; do
  printf 'Lane: %s\n' "$LANE"
  printf '%s\n' "$CANDIDATE_TASKS_JSON" | jq -r --arg lane "$LANE" '
    .[]
    | select(.lane == $lane)
    | "- #\(.number) `\(.task_ref)`: \(.title)\n  URL: \(.url)\n  Agent: \(.agent)\n  Depends on: \(.depends_on)\n  Unblocks: \(.unblocks)\n  Labels: \(.labels | join(", "))"
  '
  printf '\n'
done

if [ "$MALFORMED_COUNT" != "0" ]; then
  printf 'Malformed candidates missing Task ID:\n'
  printf '%s\n' "$CANDIDATE_TASKS_JSON" | jq -r '
    .[]
    | select(.task_id_missing)
    | "- #\(.number): \(.title)"
  '
fi

SELECTED_TASK_JSON="$(printf '%s\n' "$CANDIDATE_TASKS_JSON" | jq '.[0]')"
SELECTED_ISSUE_NUMBER="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.number')"
SELECTED_TASK_ID="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.task_id')"
SELECTED_TASK_ID_MISSING="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.task_id_missing')"
SELECTED_TITLE="$(printf '%s\n' "$SELECTED_TASK_JSON" | jq -r '.title')"

if [ "$SELECTED_TASK_ID_MISSING" = "true" ] || [ -z "$SELECTED_TASK_ID" ]; then
  printf 'error: refusing selected issue #%s because Task ID metadata is missing\n' "$SELECTED_ISSUE_NUMBER" >&2
  exit 1
fi

if [ "$PREPARE_PROMPT_MODE" = "true" ]; then
  ARTIFACT_NAME="task-prompt-$SELECTED_TASK_ID"
  ARTIFACTS_JSON="$(gh api "repos/$CARBONOPS_API_REPOSITORY/actions/artifacts?per_page=$LIMIT")"
  ARTIFACT_ID="$(printf '%s\n' "$ARTIFACTS_JSON" | jq -r --arg name "$ARTIFACT_NAME" '
    [.artifacts[] | select(.name == $name)]
    | sort_by(.created_at)
    | reverse
    | first
    | .id // empty
  ')"

  if [ -n "$ARTIFACT_ID" ]; then
    printf '\nPrompt artifact found: %s (%s)\n' "$ARTIFACT_NAME" "$ARTIFACT_ID"
    bash "$SCRIPT_DIR/download-task-prompt-artifact.sh" "$ARTIFACT_ID"
    printf '\nPrompt preparation completed without Codex execution.\n'
    exit 0
  fi

  printf '\nPrompt artifact not found: %s\n' "$ARTIFACT_NAME"
  printf 'Triggering task-prompt-handoff-generator.yml for issue #%s...\n' "$SELECTED_ISSUE_NUMBER"

  gh workflow run task-prompt-handoff-generator.yml \
    --repo "$CARBONOPS_API_REPOSITORY" \
    --ref develop \
    -f issue_number="$SELECTED_ISSUE_NUMBER"

  cat <<PROMPT_TRIGGERED

Prompt handoff generation was triggered.

Issue: #$SELECTED_ISSUE_NUMBER
Task ID: $SELECTED_TASK_ID
Expected artifact: $ARTIFACT_NAME

Rerun this command after the workflow artifact is available:

bash scripts/ops/local-worker-run-once.sh --prepare-prompt --issue $SELECTED_ISSUE_NUMBER

Codex invoked: no
Branches created: 0
Commits created: 0
Pull requests opened: 0
PROMPT_TRIGGERED
  exit 0
fi

if [ "$RUN_CODEX_MODE" = "true" ]; then
  PROMPT_PATH="$(find_prepared_prompt_path "$SELECTED_TASK_ID" || true)"

  if [ -z "$PROMPT_PATH" ]; then
    printf 'error: no prepared prompt found for task %s; run --prepare-prompt first\n' "$SELECTED_TASK_ID" >&2
    exit 1
  fi

  LOG_ROOT="$CARBONOPS_API_REPO_ROOT/.agent-handoff/logs"
  mkdir -p "$LOG_ROOT"
  TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
  LOG_PATH="$LOG_ROOT/$SELECTED_TASK_ID-$TIMESTAMP.log"
  LAST_MESSAGE_PATH="$LOG_ROOT/$SELECTED_TASK_ID-$TIMESTAMP-last-message.md"

  cat <<CODEX_START

Running local Codex CLI.

Issue: #$SELECTED_ISSUE_NUMBER
Task ID: $SELECTED_TASK_ID
Prompt path: $PROMPT_PATH
Log path: $LOG_PATH
Last message path: $LAST_MESSAGE_PATH
Working root: $CARBONOPS_API_REPO_ROOT
Sandbox: workspace-write

No branch, commit, push, pull request, or issue review transition will be performed by this worker mode.
CODEX_START

  set +e
  codex exec \
    --cd "$CARBONOPS_API_REPO_ROOT" \
    --sandbox workspace-write \
    --output-last-message "$LAST_MESSAGE_PATH" \
    - < "$PROMPT_PATH" 2>&1 | tee "$LOG_PATH"
  CODEX_STATUS="${PIPESTATUS[0]}"
  set -e

  if [ "$CODEX_STATUS" -ne 0 ]; then
    printf '\nerror: local Codex CLI exited with status %s\n' "$CODEX_STATUS" >&2
    printf 'Log path: %s\n' "$LOG_PATH" >&2
    exit "$CODEX_STATUS"
  fi

  cat <<CODEX_DONE

Local Codex CLI completed.

Issue: #$SELECTED_ISSUE_NUMBER
Task ID: $SELECTED_TASK_ID
Log path: $LOG_PATH
Last message path: $LAST_MESSAGE_PATH
Branches created by worker: 0
Commits created by worker: 0
Pull requests opened by worker: 0
Issue status changed by worker: no
CODEX_DONE
  exit 0
fi

if [ "$CLAIM_MODE" != "true" ]; then
  printf '\nDry-run completed without GitHub mutations.\n'
  exit 0
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
