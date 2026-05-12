#!/usr/bin/env bash
set -euo pipefail

LIMIT="${LIMIT:-100}"

fail() {
  printf 'error: %s\n' "$*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "required command not found: $1"
}

require_command gh
require_command jq

gh auth status >/dev/null 2>&1 || fail "gh is not authenticated; run: gh auth login"

REPOSITORY="${REPOSITORY:-$(gh repo view --json nameWithOwner --jq '.nameWithOwner')}"

printf 'Repository: %s\n' "$REPOSITORY"
printf 'Listing recent task prompt artifacts whose names start with task-prompt-.\n\n'

ARTIFACTS_JSON="$(gh api "repos/$REPOSITORY/actions/artifacts?per_page=$LIMIT")"

MATCH_COUNT="$(printf '%s\n' "$ARTIFACTS_JSON" | jq '[.artifacts[] | select(.name | startswith("task-prompt-"))] | length')"

if [ "$MATCH_COUNT" = "0" ]; then
  printf 'No task prompt artifacts found in the most recent %s workflow artifacts.\n' "$LIMIT"
  exit 0
fi

printf '%-14s %-36s %-12s %-22s %-22s\n' "ARTIFACT_ID" "NAME" "SIZE_BYTES" "CREATED_AT" "UPDATED_AT"

printf '%s\n' "$ARTIFACTS_JSON" |
  jq -r '
    [.artifacts[] | select(.name | startswith("task-prompt-"))]
    | sort_by(.created_at)
    | reverse
    | .[]
    | [
        (.id | tostring),
        .name,
        (.size_in_bytes | tostring),
        (.created_at // "unknown"),
        (.updated_at // "unknown")
      ]
    | @tsv
  ' |
  while IFS=$'\t' read -r artifact_id name size_bytes created_at updated_at; do
    printf '%-14s %-36s %-12s %-22s %-22s\n' "$artifact_id" "$name" "$size_bytes" "$created_at" "$updated_at"
  done
