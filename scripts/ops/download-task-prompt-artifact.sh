#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$SCRIPT_DIR/lib/repo-guard.sh"

LIMIT="${LIMIT:-100}"

fail() {
  printf 'error: %s\n' "$*" >&2
  exit 1
}

usage() {
  cat <<'EOF'
Usage:
  scripts/ops/download-task-prompt-artifact.sh <artifact-id|task-id>

Examples:
  scripts/ops/download-task-prompt-artifact.sh 1234567890
  scripts/ops/download-task-prompt-artifact.sh API-OPS-006
  scripts/ops/download-task-prompt-artifact.sh task-prompt-API-OPS-006
EOF
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "required command not found: $1"
}

if [ "${1:-}" = "-h" ] || [ "${1:-}" = "--help" ]; then
  usage
  exit 0
fi

QUERY="${1:-}"
[ -n "$QUERY" ] || {
  usage >&2
  fail "artifact id or task id is required"
}

require_command gh
require_command jq
require_command unzip

carbonops_api_repo_guard_init

gh auth status >/dev/null 2>&1 || fail "gh is not authenticated; run: gh auth login"

REPOSITORY="$CARBONOPS_API_REPOSITORY"
DOWNLOAD_ROOT="$CARBONOPS_API_REPO_ROOT/.agent-handoff/downloads"

if printf '%s' "$QUERY" | grep -Eq '^[0-9]+$'; then
  ARTIFACT_ID="$QUERY"
  ARTIFACT_JSON="$(gh api "repos/$REPOSITORY/actions/artifacts/$ARTIFACT_ID")"
else
  if printf '%s' "$QUERY" | grep -Eq '^task-prompt-'; then
    ARTIFACT_NAME="$QUERY"
  else
    ARTIFACT_NAME="task-prompt-$QUERY"
  fi

  ARTIFACTS_JSON="$(gh api "repos/$REPOSITORY/actions/artifacts?per_page=$LIMIT")"
  ARTIFACT_JSON="$(printf '%s\n' "$ARTIFACTS_JSON" |
    jq -c --arg name "$ARTIFACT_NAME" '
      [.artifacts[] | select(.name == $name)]
      | sort_by(.created_at)
      | reverse
      | first // empty
    ')"

  [ -n "$ARTIFACT_JSON" ] || fail "no artifact named $ARTIFACT_NAME found in the most recent $LIMIT workflow artifacts"
  ARTIFACT_ID="$(printf '%s\n' "$ARTIFACT_JSON" | jq -r '.id')"
fi

ARTIFACT_NAME="$(printf '%s\n' "$ARTIFACT_JSON" | jq -r '.name')"
[ "$ARTIFACT_NAME" != "null" ] && [ -n "$ARTIFACT_NAME" ] || fail "artifact $ARTIFACT_ID was not found"

DEST_DIR="$DOWNLOAD_ROOT/$ARTIFACT_NAME-$ARTIFACT_ID"
ZIP_PATH="$DEST_DIR/artifact.zip"

rm -rf "$DEST_DIR"
mkdir -p "$DEST_DIR"

printf 'Downloading artifact %s (%s) from %s...\n' "$ARTIFACT_ID" "$ARTIFACT_NAME" "$REPOSITORY"
gh api "repos/$REPOSITORY/actions/artifacts/$ARTIFACT_ID/zip" > "$ZIP_PATH"

unzip -q "$ZIP_PATH" -d "$DEST_DIR"
rm -f "$ZIP_PATH"

PROMPT_PATH="$(find "$DEST_DIR" -type f -name '*-prompt.md' | sort | head -n 1)"
METADATA_PATH="$(find "$DEST_DIR" -type f -name '*-metadata.json' | sort | head -n 1)"

[ -n "$PROMPT_PATH" ] || fail "downloaded artifact did not contain a *-prompt.md file"

printf 'Artifact extracted to: %s\n' "$DEST_DIR"
printf 'Prompt path: %s\n' "$PROMPT_PATH"

if [ -n "$METADATA_PATH" ]; then
  printf 'Metadata path: %s\n' "$METADATA_PATH"
else
  printf 'Metadata path: not found\n'
fi
