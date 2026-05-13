# Local Worker Run-Once

CarbonOps-API uses GitHub issues as the task queue and GitHub Actions as the
prompt/validation surface. The local worker is the MBP 2015 execution boundary
for future local Codex automation when cloud-side PR creation is not reliable.

This document covers the first worker phase only: dry-run discovery.

## Current Phase

`local-worker-run-once.sh` is dry-run only.

It can:

- verify that it is running inside the `ktalpay/CarbonOps-API` checkout;
- refuse execution from a sibling CarbonOps-Parser checkout or another repo;
- acquire a local worker lock so concurrent runs are rejected;
- read open GitHub issues labeled `status:ready`;
- parse task metadata such as Task ID, Lane, Agent, Depends on, and Unblocks;
- print a deterministic summary grouped by lane.

It does not:

- claim issues;
- add or remove labels;
- assign issues;
- comment on issues;
- download prompt artifacts;
- run Codex;
- create branches;
- commit or push;
- open pull requests.

## Usage

Run from anywhere inside the CarbonOps-API checkout:

```bash
scripts/ops/local-worker-run-once.sh
```

Limit the number of ready issues read from GitHub:

```bash
scripts/ops/local-worker-run-once.sh --limit 25
```

Filter by lane metadata or lane label:

```bash
scripts/ops/local-worker-run-once.sh --lane ops
scripts/ops/local-worker-run-once.sh --lane dotnet
```

## Required Local Tools

The dry-run worker requires:

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

1. claim mode: move `status:ready` tasks to `status:in-progress`;
2. prompt artifact lookup and download;
3. local Codex CLI execution with timeout and log capture;
4. branch, commit, push, and PR creation;
5. failure handling with `status:needs-fix` and issue comments;
6. optional LaunchAgent or scheduled runner setup.

Each phase must remain idempotent and must keep GitHub as the source of truth for
queue state.
