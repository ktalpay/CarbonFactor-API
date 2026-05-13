#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/ops/lib/repo-guard.sh
source "$SCRIPT_DIR/lib/repo-guard.sh"
# shellcheck source=scripts/ops/lib/worker-lock.sh
source "$SCRIPT_DIR/lib/worker-lock.sh"

usage() {
  cat <<'USAGE'
Usage:
  bash scripts/ops/local-worker-in-progress-safety.sh --list-in-progress [--limit <count>]
  bash scripts/ops/local-worker-in-progress-safety.sh --mark-needs-fix --issue <number>

This helper is conservative. It does not run Codex, create branches, commit,
push, or open pull requests.
USAGE
}

MODE=""
ISSUE_NUMBER=""
LIMIT="50"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --list-in-progress)
      MODE="list"
      shift
      ;;
    --mark-needs-fix)
      MODE="mark"
      shift
      ;;
    --issue)
      if [ "$#" -lt 2 ]; then
        printf 'error: --issue requires a value\n' >&2
        exit 1
      fi
      ISSUE_NUMBER="$2"
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

if [ -z "$MODE" ]; then
  usage >&2
  exit 1
fi

case "$LIMIT" in
  ''|*[!0-9]*)
    printf 'error: --limit must be a positive integer\n' >&2
    exit 1
    ;;
esac

if [ -n "$ISSUE_NUMBER" ]; then
  case "$ISSUE_NUMBER" in
    ''|*[!0-9]*)
      printf 'error: --issue must be a positive integer\n' >&2
      exit 1
      ;;
  esac
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

if [ "$MODE" = "list" ]; then
  ISSUES_JSON="$(gh issue list \
    --repo "$CARBONOPS_API_REPOSITORY" \
    --search 'label:"status:in-progress" state:open' \
    --limit "$LIMIT" \
    --json number,title,body,url,labels,updatedAt)"

  COUNT="$(printf '%s\n' "$ISSUES_JSON" | jq 'length')"

  cat <<SUMMARY
CarbonOps-API in-progress task safety listing

Repository: $CARBONOPS_API_REPOSITORY
Repo root: $CARBONOPS_API_REPO_ROOT
Limit: $LIMIT
Candidate count: $COUNT
Mutation mode: disabled
Codex invoked: no
Branches created: 0
Commits created: 0
Pull requests opened: 0
SUMMARY

  if [ "$COUNT" = "0" ]; then
    printf '\nNo in-progress task candidates found.\n'
    exit 0
  fi

  printf '\nIn-progress task candidates:\n\n'
  printf '%s\n' "$ISSUES_JSON" | jq -r '
    def field($name):
      [(.body // "" | split("\n")[] | (try capture("^\\s*" + $name + "\\s*:\\s*(?<value>.*)\\s*$"; "i").value catch empty))]
      | first
      | . // "";

    .[]
    | "- #\(.number) `\(field("Task ID"))`: \(.title)\n  URL: \(.url)\n  Updated: \(.updatedAt // "unknown")\n  Labels: \([(.labels // [])[]?.name] | join(", "))"
  '
  exit 0
fi

if [ "$MODE" = "mark" ]; then
  if [ -z "$ISSUE_NUMBER" ]; then
    printf 'error: --mark-needs-fix requires --issue <number>\n' >&2
    exit 1
  fi

  ISSUE_JSON="$(gh issue view "$ISSUE_NUMBER" \
    --repo "$CARBONOPS_API_REPOSITORY" \
    --json number,title,body,url,labels,state)"

  STATE="$(printf '%s\n' "$ISSUE_JSON" | jq -r '.state')"
  HAS_IN_PROGRESS="$(printf '%s\n' "$ISSUE_JSON" | jq -r 'any((.labels // [])[]?.name; . == "status:in-progress")')"
  TASK_ID="$(printf '%s\n' "$ISSUE_JSON" | jq -r '.body // ""' | extract_task_id_from_body)"

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

  COMMENT="$(cat <<COMMENT_BODY
Local worker safety handling moved this task from \`status:in-progress\` to \`status:needs-fix\`.

Task ID: \`$TASK_ID\`
Reason: explicit selected-issue handling was requested for an in-progress task.

No Codex execution, branch creation, commit, push, or pull request creation was performed by this step.
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

In-progress task marked as needs-fix.

Issue: #$ISSUE_NUMBER
Task ID: $TASK_ID
New status label: status:needs-fix
Codex invoked: no
Branches created: 0
Commits created: 0
Pull requests opened: 0
DONE
  exit 0
fi

usage >&2
exit 1
