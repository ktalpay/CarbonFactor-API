#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/ops/lib/repo-guard.sh
source "$SCRIPT_DIR/lib/repo-guard.sh"

usage() {
  cat <<'USAGE'
Usage: bash scripts/ops/install-local-worker-launchagent.sh [--lane <lane>] [--interval-seconds <seconds>] [--dry-run]

Installs a per-user macOS LaunchAgent that periodically runs:
  scripts/ops/local-worker-auto-run-once.sh --lane <lane>

Defaults:
  lane: dotnet
  interval: 900 seconds

The installer does not store secrets. It writes a plist under ~/Library/LaunchAgents
and writes worker logs under .agent-handoff/logs/.
USAGE
}

LANE="dotnet"
INTERVAL_SECONDS="900"
DRY_RUN="false"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --lane)
      if [ "$#" -lt 2 ]; then
        printf 'error: --lane requires a value\n' >&2
        exit 1
      fi
      LANE="$2"
      shift 2
      ;;
    --interval-seconds)
      if [ "$#" -lt 2 ]; then
        printf 'error: --interval-seconds requires a value\n' >&2
        exit 1
      fi
      INTERVAL_SECONDS="$2"
      shift 2
      ;;
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

case "$INTERVAL_SECONDS" in
  ''|*[!0-9]*)
    printf 'error: --interval-seconds must be a positive integer\n' >&2
    exit 1
    ;;
esac

if [ "$INTERVAL_SECONDS" -lt 60 ]; then
  printf 'error: --interval-seconds must be at least 60\n' >&2
  exit 1
fi

case "$LANE" in
  ops|dotnet|api|python|pt)
    ;;
  *)
    printf 'error: unsupported lane: %s\n' "$LANE" >&2
    exit 1
    ;;
esac

command -v launchctl >/dev/null 2>&1 || {
  printf 'error: required command not found: launchctl\n' >&2
  exit 1
}

carbonops_api_repo_guard_init

REPO_ROOT="$CARBONOPS_API_REPO_ROOT"
LABEL="com.carbonops.api.local-worker"
PLIST_DIR="$HOME/Library/LaunchAgents"
PLIST_PATH="$PLIST_DIR/$LABEL.plist"
LOG_DIR="$REPO_ROOT/.agent-handoff/logs"
STDOUT_LOG="$LOG_DIR/local-worker-launchagent.out.log"
STDERR_LOG="$LOG_DIR/local-worker-launchagent.err.log"
RUN_COMMAND="cd '$REPO_ROOT' && bash scripts/ops/local-worker-auto-run-once.sh --lane '$LANE'"

mkdir -p "$LOG_DIR"

cat > /tmp/carbonops-api-local-worker.plist <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key>
  <string>$LABEL</string>
  <key>ProgramArguments</key>
  <array>
    <string>/bin/bash</string>
    <string>-lc</string>
    <string>$RUN_COMMAND</string>
  </array>
  <key>WorkingDirectory</key>
  <string>$REPO_ROOT</string>
  <key>StartInterval</key>
  <integer>$INTERVAL_SECONDS</integer>
  <key>RunAtLoad</key>
  <false/>
  <key>StandardOutPath</key>
  <string>$STDOUT_LOG</string>
  <key>StandardErrorPath</key>
  <string>$STDERR_LOG</string>
</dict>
</plist>
PLIST

cat <<SUMMARY
CarbonOps-API local worker LaunchAgent install plan

Label: $LABEL
Repository: $REPO_ROOT
Lane: $LANE
Interval seconds: $INTERVAL_SECONDS
Plist path: $PLIST_PATH
Stdout log: $STDOUT_LOG
Stderr log: $STDERR_LOG
Dry run: $DRY_RUN
SUMMARY

if [ "$DRY_RUN" = "true" ]; then
  printf '\nDry-run plist content:\n\n'
  cat /tmp/carbonops-api-local-worker.plist
  rm -f /tmp/carbonops-api-local-worker.plist
  exit 0
fi

mkdir -p "$PLIST_DIR"

if launchctl print "gui/$(id -u)/$LABEL" >/dev/null 2>&1; then
  launchctl bootout "gui/$(id -u)" "$PLIST_PATH" >/dev/null 2>&1 || true
fi

cp /tmp/carbonops-api-local-worker.plist "$PLIST_PATH"
rm -f /tmp/carbonops-api-local-worker.plist

launchctl bootstrap "gui/$(id -u)" "$PLIST_PATH"
launchctl enable "gui/$(id -u)/$LABEL"

cat <<INSTALLED

LaunchAgent installed.

To run once now:
  launchctl kickstart -k gui/$(id -u)/$LABEL

To inspect:
  launchctl print gui/$(id -u)/$LABEL

To uninstall:
  bash scripts/ops/uninstall-local-worker-launchagent.sh
INSTALLED
