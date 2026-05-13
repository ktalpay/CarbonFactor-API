#!/usr/bin/env bash
set -euo pipefail

LABEL="com.carbonops.api.local-worker"
PLIST_PATH="$HOME/Library/LaunchAgents/$LABEL.plist"

usage() {
  cat <<'USAGE'
Usage: bash scripts/ops/uninstall-local-worker-launchagent.sh [--dry-run]

Unloads and removes the CarbonOps-API local worker LaunchAgent for the current
macOS user.
USAGE
}

DRY_RUN="false"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --dry-run)
      DRY_RUN="true"
      shift
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

command -v launchctl >/dev/null 2>&1 || {
  printf 'error: required command not found: launchctl\n' >&2
  exit 1
}

cat <<SUMMARY
CarbonOps-API local worker LaunchAgent uninstall plan

Label: $LABEL
Plist path: $PLIST_PATH
Dry run: $DRY_RUN
SUMMARY

if [ "$DRY_RUN" = "true" ]; then
  exit 0
fi

if launchctl print "gui/$(id -u)/$LABEL" >/dev/null 2>&1; then
  launchctl bootout "gui/$(id -u)" "$PLIST_PATH" >/dev/null 2>&1 || true
fi

if [ -f "$PLIST_PATH" ]; then
  rm -f "$PLIST_PATH"
fi

cat <<DONE

LaunchAgent removed if it existed.

To confirm:
  launchctl print gui/$(id -u)/$LABEL

Expected after uninstall:
  launchctl returns a non-zero status for this label.
DONE
