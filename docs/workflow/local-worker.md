# Local Worker Run-Once

CarbonOps-API uses GitHub issues as the task queue and GitHub Actions as the
prompt/validation surface. The local worker is the MBP 2015 execution boundary
for future local Codex automation when cloud-side PR creation is not reliable.

This document covers the current worker phases: dry-run discovery, explicit
claim mode, prompt artifact preparation, local Codex execution, and PR creation.

## Current Phase

`local-worker-run-once.sh` defaults to dry-run only.

It can:

- verify that it is running inside the `ktalpay/CarbonOps-API` checkout;
- refuse execution from a sibling CarbonOps-Parser checkout or another repo;
- acquire a local worker lock so concurrent runs are rejected;
- read open GitHub issues labeled `status:ready` or `status:in-progress`;
- parse task metadata such as Task ID, Lane, Agent, Depends on, and Unblocks;
- print a deterministic summary grouped by lane;
- optionally claim exactly one eligible ready task when `--claim` is provided;
- optionally prepare a prompt artifact when `--prepare-prompt` is provided;
- optionally run local Codex against an already prepared prompt when `--run-codex` is provided;
- optionally open a pull request for local task changes when `--open-pr` is provided.

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

Prompt preparation mode may:

- detect an existing `task-prompt-<TASK-ID>` artifact;
- download and extract the prompt artifact under `.agent-handoff/downloads/`;
- trigger `task-prompt-handoff-generator.yml` when no matching artifact exists.

Prompt preparation mode does not:

- run Codex;
- create branches;
- commit or push;
- open pull requests.

Codex execution mode may:

- locate a previously prepared prompt under `.agent-handoff/downloads/`;
- run `codex exec` with the prompt provided through stdin;
- write local logs under `.agent-handoff/logs/`;
- write the last Codex message under `.agent-handoff/logs/`.

The worker does not enforce a portable timeout around `codex exec`. If the local
Codex process runs too long, stop it with `Ctrl-C`; the worker lock is released
by the exit trap, and the printed log path remains under `.agent-handoff/logs/`
for inspection.

Codex execution mode does not:

- create branches;
- commit or push;
- open pull requests;
- move issues to review-ready status.

PR mode may:

- create a task branch when running from a detached worktree;
- commit current non-generated task changes;
- push the task branch to origin;
- open a pull request into `develop`;
- move the linked issue from `status:in-progress` to `status:in-review` after PR creation succeeds.

PR mode does not:

- merge pull requests;
- bypass CI;
- run Codex;
- include `.agent-handoff/`, `bin/`, `obj/`, Python cache, or package metadata artifacts.

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
bash scripts/ops/local-worker-run-once.sh --issue 39 --limit 10
```

Claim a deliberately selected ready issue:

```bash
bash scripts/ops/local-worker-run-once.sh --claim --issue 39
```

When `--claim` is used, the worker verifies that the selected issue is still
open, still labeled `status:ready`, and still has `Task ID:` metadata. It then
removes `status:ready`, adds `status:in-progress`, and writes a claim comment.

Prepare the prompt artifact for a selected task:

```bash
bash scripts/ops/local-worker-run-once.sh --prepare-prompt --issue 39
```

If the matching artifact already exists, the worker downloads and extracts it
under `.agent-handoff/downloads/` and prints the prompt path. If the artifact is
missing, the worker triggers prompt handoff generation and prints a rerun-later
message.

Run local Codex against a prepared prompt:

```bash
bash scripts/ops/local-worker-run-once.sh --run-codex --issue 41
```

`--run-codex` requires a prepared prompt artifact. Run `--prepare-prompt` first
if the worker reports that no prepared prompt exists. The worker calls
`codex exec --cd <repo-root> --sandbox workspace-write -` and feeds the prompt
through stdin.

This mode is an execution boundary only. It captures local Codex output and the
last Codex message under `.agent-handoff/logs/`, but it does not create a task
branch, commit, push, open a pull request, or mark the issue review-ready.

Open a PR for task changes:

```bash
bash scripts/ops/local-worker-run-once.sh --open-pr --issue 43
```

`--open-pr` requires the selected issue to be `status:in-progress`. It refuses to
run directly on `main` or `develop`, refuses generated/local artifact paths,
commits current task changes, pushes the task branch, opens a PR to `develop`,
and only then moves the issue to `status:in-review`.

## Required Local Tools

The worker requires:

- `git`
- `gh`
- `jq`
- `bash`
- `unzip` for prompt preparation downloads
- `codex` for local Codex execution

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

## Generated Artifact Guard

Downloaded prompt artifacts and local Codex logs are local execution inputs and
outputs only. Do not commit `.agent-handoff/` contents.

PR mode also refuses common generated paths such as `bin/`, `obj/`,
`__pycache__/`, `.pytest_cache/`, `.egg-info/`, and `.pyc` files.

## Future Phases

Later tasks should add these capabilities incrementally:

1. failure handling with `status:needs-fix` and issue comments;
2. stale `status:in-progress` recovery;
3. optional LaunchAgent or scheduled runner setup.

Each phase must remain idempotent and must keep GitHub as the source of truth for
queue state.
