#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
image_tag="${IMAGE_TAG:-carbonops-api:ops-032}"

usage() {
  cat <<'USAGE'
Usage: scripts/ops/validate-dotnet-package.sh [--check-only]

Builds the CarbonOps-API Docker image from the repository root.

Options:
  --check-only   Validate packaging prerequisites without building an image.
USAGE
}

check_prerequisites() {
  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required to validate the container package." >&2
    return 127
  fi

  if [ ! -f "$repo_root/Dockerfile" ]; then
    echo "Dockerfile was not found at the repository root." >&2
    return 1
  fi
}

case "${1:-}" in
  "")
    ;;
  "--check-only")
    check_prerequisites
    echo "Packaging prerequisites are available. No image was built."
    exit 0
    ;;
  "-h" | "--help")
    usage
    exit 0
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac

check_prerequisites

cd "$repo_root"
docker build -f Dockerfile -t "$image_tag" .
echo "Built Docker image: $image_tag"
