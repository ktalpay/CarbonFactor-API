#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$SCRIPT_DIR/lib/repo-guard.sh"

fail() {
  printf 'error: %s\n' "$*" >&2
  exit 1
}

usage() {
  cat <<'EOF'
Usage:
  scripts/ops/prepare-codex-task-run.sh <prompt-file>

The script validates the prompt file and prints the recommended Codex CLI
command. It does not execute Codex, create branches, commit, push, or open PRs.
EOF
}

if [ "${1:-}" = "-h" ] || [ "${1:-}" = "--help" ]; then
  usage
  exit 0
fi

PROMPT_PATH="${1:-}"
[ -n "$PROMPT_PATH" ] || {
  usage >&2
  fail "prompt file path is required"
}

carbonops_api_repo_guard_init

[ -f "$PROMPT_PATH" ] || fail "prompt file does not exist: $PROMPT_PATH"

PROMPT_DIR="$(cd "$(dirname "$PROMPT_PATH")" && pwd -P)"
PROMPT_BASENAME="$(basename "$PROMPT_PATH")"
PROMPT_ABSOLUTE_PATH="$PROMPT_DIR/$PROMPT_BASENAME"

case "$PROMPT_ABSOLUTE_PATH" in
  "$CARBONOPS_API_REPO_ROOT"/*)
    ;;
  *)
    fail "prompt file must be inside the CarbonOps-API repo root: $CARBONOPS_API_REPO_ROOT"
    ;;
esac

PROMPT_SIZE_BYTES="$(wc -c < "$PROMPT_PATH" | tr -d ' ')"
[ "$PROMPT_SIZE_BYTES" -gt 0 ] || fail "prompt file is empty: $PROMPT_PATH"

printf 'Repository: %s\n' "$CARBONOPS_API_REPOSITORY"
printf 'Repo root: %s\n' "$CARBONOPS_API_REPO_ROOT"
printf 'Prompt file: %s\n' "$PROMPT_ABSOLUTE_PATH"
printf 'Prompt size: %s bytes\n' "$PROMPT_SIZE_BYTES"

if command -v codex >/dev/null 2>&1; then
  printf 'Codex CLI: found at %s\n' "$(command -v codex)"
else
  printf 'Codex CLI: not found on PATH. Install and authenticate Codex CLI before running the command below.\n'
fi

cat <<EOF

Recommended Codex CLI command:

  codex exec < "$PROMPT_ABSOLUTE_PATH"

Expected behavior:

- The generated prompt instructs Codex to create the task feature branch.
- Codex should make the scoped changes, run validation, commit, push, and open a PR to develop.
- Codex must not merge the PR.
- The user remains responsible for PR review and merge.

This helper did not execute Codex or mutate local/git/GitHub state.
EOF
