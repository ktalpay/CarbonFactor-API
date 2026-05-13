# Local Worker Run-Once

CarbonOps-API uses GitHub issues as the task queue and GitHub Actions as the
prompt/validation surface. The local worker is the MBP 2015 execution boundary
for future local Codex automation when cloud-side PR creation is not reliable.

This document covers the current worker phases: dry-run discovery and explicit
claim mode.

## Current Phase

`local-worker-run-once.sh` defaults to dry-run only.

It can:

- verify that it is running inside the `ktalpay/CarbonOps-API` checkout;
- refuse execution from a sibling CarbonOps-Parser checkout or another repo;
- acquire a local worker lock so concurrent runs are rejected;
- read open GitHub issues labeled `status:ready`;
- parse task metadata such as Task ID, Lane, Agent, Depends on, and Unblocks;
- print a deterministic summary grouped by lane;
- optionally claim exactly one eligible ready task when `--claim` is provided.

Dry-run mode does not:

- claim issues;
- add or remove labels;
- assign issues;
- comment on issues;
- download prompt artifacts;
- run Codex;
- create branches;
- commit or push;
- open pull requests.

Claim mode does not:

- download prompt artifacts;
- run Codex;
- create branches;
- commit or push;
- open pull requests.

## Usage

Run from anywhere inside the CarbonOps-API checkout. Use `bash` explicitly so the
script does not depend on executable file mode after GitHub API-created commits:

```bash
bash scripts/ops/local-worker-run-once.sh
```

Limit the number of ready issues read from GitHub:

```bash
bash scripts/ops/local-worker-run-once.sh --limit 25
```

Filter by lane metadata or lane label:

```bash
bash scripts/ops/local-worker-run-once.sh --lane ops
bash scripts/ops/local-worker-run-once.sh --lane dotnet
```

Filter to a specific issue without mutating state:

```bash
bash scripts/ops/local-worker-run-once.sh --issue 37 --limit 10
```

Claim a deliberately selected ready issue:

```bash
bash scripts/ops/local-worker-run-once.sh --claim --issue 37
```

When `--claim` is used, the worker verifies that the selected issue is still
open, still labeled `status:ready`, and still has `Task ID:` metadata. It then
removes `status:ready`, adds `status:in-progress`, and writes a claim comment.

## Required Local Tools

The worker requires:

- `git`
- `gh`
- `jq`
- `bash`

The GitHub CLI must already be authenticated with access to
`ktalpay/CarbonOps-API`.

## Repository Guard

The worker sources `scripts/ops/lib/repo-guard.sh` and refuses to run unless the
current checkout has an `origin` remote matching `ktalpay/CarbonOps-API`.

This is deliberate because the same MBP also hosts CarbonOps-Parser workflows.
Running CarbonOps-API automation from the Parser checkout must fail fast.

## Lock Guard

The worker creates a lock directory under the repository Git directory. A second
worker instance fails while the lock exists.

The lock is removed automatically when the process exits normally or receives a
handled interrupt/termination signal.

## Future Phases

Later tasks should add these capabilities incrementally:

1. prompt artifact lookup and download;
2. local Codex CLI execution with timeout and log capture;
3. branch, commit, push, and PR creation;
4. failure handling with `status:needs-fix` and issue comments;
5. stale `status:in-progress` recovery;
6. optional LaunchAgent or scheduled runner setup.

Each phase must remain idempotent and must keep GitHub as the source of truth for
queue state.
