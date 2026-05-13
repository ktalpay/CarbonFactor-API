#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/repo-guard.sh"
source "$SCRIPT_DIR/lib/worker-lock.sh"

usage() {
  cat <<'USAGE'
Usage:
  bash scripts/ops/local-worker-status-report.sh --issue <number> --phase <phase> --summary <text> [--log-path <path>]

Adds a local worker status report comment to a selected status:in-progress task
and moves it to status:needs-fix.

This helper does not run Codex, create branches, commit, push, or open pull
requests.
USAGE
}

ISSUE_NUMBER=""
PHASE=""
SUMMARY=""
LOG_PATH=""

while [ "$#" -gt 0 ]; do
  case "$1" in
    --issue)
      ISSUE_NUMBER="${2:-}"
      shift 2
      ;;
    --phase)
      PHASE="${2:-}"
      shift 2
      ;;
    --summary)
      SUMMARY="${2:-}"
      shift 2
      ;;
    --log-path)
      LOG_PATH="${2:-}"
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

if [ -z "$ISSUE_NUMBER" ] || [ -z "$PHASE" ] || [ -z "$SUMMARY" ]; then
  usage >&2
  exit 1
fi

case "$ISSUE_NUMBER" in
  ''|*[!0-9]*)
    printf 'error: --issue must be a positive integer\n' >&2
    exit 1
    ;;
esac

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

ISSUE_JSON="$(gh issue view "$ISSUE_NUMBER" \
  --repo "$CARBONOPS_API_REPOSITORY" \
  --json number,title,body,url,labels,state)"

STATE="$(printf '%s\n' "$ISSUE_JSON" | jq -r '.state')"
HAS_IN_PROGRESS="$(printf '%s\n' "$ISSUE_JSON" | jq -r 'any((.labels // [])[]?.name; . == "status:in-progress")')"
TASK_ID="$(printf '%s\n' "$ISSUE_JSON" | jq -r '.body // ""' | extract_task_id_from_body)"
TITLE="$(printf '%s\n' "$ISSUE_JSON" | jq -r '.title')"

if [ "$STATE" != "OPEN" ]; then
  printf 'error: refusing selected issue #%s because it is not open\n' "$ISSUE_NUMBER" >&2
  exit 1
fi

if [ "$HAS_IN_PROGRESS" != "true" ]; then
  printf 'error: refusing selected issue #%s because it is not status:in-progress\n' "$ISSUE_NUMBER" >&2
  exit 1
fi

if [ -z "$TASK_ID" ]; then
  printf 'error: refusing selected issue #%s because Task ID metadata is missing\n' "$ISSUE_NUMBER" >&2
  exit 1
fi

if [ -n "$LOG_PATH" ]; then
  LOG_LINE="Local log path: \`$LOG_PATH\`"
else
  LOG_LINE="Local log path: not provided"
fi

COMMENT="$(cat <<COMMENT_BODY
Local worker status report moved this task from \`status:in-progress\` to \`status:needs-fix\`.

Task ID: \`$TASK_ID\`
Phase: \`$PHASE\`
Summary: $SUMMARY
$LOG_LINE

No Codex execution, branch creation, commit, push, or pull request creation was performed by this report step.
COMMENT_BODY
)"

gh issue edit "$ISSUE_NUMBER" \
  --repo "$CARBONOPS_API_REPOSITORY" \
  --remove-label "status:in-progress" \
  --add-label "status:needs-fix"

gh issue comment "$ISSUE_NUMBER" \
  --repo "$CARBONOPS_API_REPOSITORY" \
  --body "$COMMENT"

cat <<DONE

Local worker status report completed.

Issue: #$ISSUE_NUMBER
Task ID: $TASK_ID
Title: $TITLE
Phase: $PHASE
New status label: status:needs-fix
Codex invoked: no
Branches created: 0
Commits created: 0
Pull requests opened: 0
DONE
