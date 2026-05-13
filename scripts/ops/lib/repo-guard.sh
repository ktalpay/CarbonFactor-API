#!/usr/bin/env bash

CARBONOPS_API_EXPECTED_REPOSITORY="ktalpay/CarbonOps-API"

repo_guard_fail() {
  printf 'error: %s\n' "$*" >&2
  exit 1
}

carbonops_api_repo_guard_init() {
  command -v git >/dev/null 2>&1 || repo_guard_fail "required command not found: git"

  local repo_root
  repo_root="$(git rev-parse --show-toplevel 2>/dev/null)" ||
    repo_guard_fail "not inside a git repository; run this script from the CarbonOps-API checkout"

  local origin_url
  origin_url="$(git -C "$repo_root" remote get-url origin 2>/dev/null)" ||
    repo_guard_fail "git remote 'origin' is missing; expected CarbonOps-API origin"

  case "$origin_url" in
    "https://github.com/ktalpay/CarbonOps-API" | \
    "https://github.com/ktalpay/CarbonOps-API.git" | \
    "git@github.com:ktalpay/CarbonOps-API" | \
    "git@github.com:ktalpay/CarbonOps-API.git" | \
    "ssh://git@github.com/ktalpay/CarbonOps-API" | \
    "ssh://git@github.com/ktalpay/CarbonOps-API.git")
      ;;
    *)
      repo_guard_fail "refusing to run in repository with origin '$origin_url'; expected ktalpay/CarbonOps-API"
      ;;
  esac

  CARBONOPS_API_REPO_ROOT="$repo_root"
  CARBONOPS_API_REPOSITORY="$CARBONOPS_API_EXPECTED_REPOSITORY"

  export CARBONOPS_API_REPO_ROOT
  export CARBONOPS_API_REPOSITORY
}
