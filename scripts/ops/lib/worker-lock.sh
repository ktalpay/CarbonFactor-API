#!/usr/bin/env bash

carbonops_api_worker_lock_fail() {
  printf 'error: %s\n' "$*" >&2
  exit 1
}

carbonops_api_worker_lock_acquire() {
  if [ -z "${CARBONOPS_API_REPO_ROOT:-}" ]; then
    carbonops_api_worker_lock_fail "CARBONOPS_API_REPO_ROOT is not set; call carbonops_api_repo_guard_init first"
  fi

  local git_dir
  git_dir="$(git -C "$CARBONOPS_API_REPO_ROOT" rev-parse --git-dir 2>/dev/null)" ||
    carbonops_api_worker_lock_fail "unable to resolve git directory for lock placement"

  case "$git_dir" in
    /*)
      ;;
    *)
      git_dir="$CARBONOPS_API_REPO_ROOT/$git_dir"
      ;;
  esac

  CARBONOPS_API_WORKER_LOCK_DIR="$git_dir/carbonops-api-local-worker.lock"

  if ! mkdir "$CARBONOPS_API_WORKER_LOCK_DIR" 2>/dev/null; then
    carbonops_api_worker_lock_fail "local worker lock already exists at $CARBONOPS_API_WORKER_LOCK_DIR; another worker may be running"
  fi

  printf '%s\n' "$$" > "$CARBONOPS_API_WORKER_LOCK_DIR/pid"
  export CARBONOPS_API_WORKER_LOCK_DIR

  trap carbonops_api_worker_lock_release EXIT INT TERM
}

carbonops_api_worker_lock_release() {
  if [ -n "${CARBONOPS_API_WORKER_LOCK_DIR:-}" ] && [ -d "$CARBONOPS_API_WORKER_LOCK_DIR" ]; then
    rm -rf "$CARBONOPS_API_WORKER_LOCK_DIR"
  fi
}
