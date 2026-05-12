# Task Prompt Handoff

The Task Prompt Handoff Generator turns a structured task issue into a
Codex-ready Markdown prompt artifact. It is a bridge between task issues and a
future local Codex execution flow.

## Workflow

The workflow is defined at:

- `.github/workflows/task-prompt-handoff-generator.yml`

It runs manually with `workflow_dispatch` and requires one input:

- `issue_number`: the GitHub issue number to read.

## Behavior

The generator reads the issue with the GitHub CLI, parses the structured task
fields, and writes:

- `.agent-handoff/<TASK-ID>-prompt.md`
- `.agent-handoff/<TASK-ID>-metadata.json`

Those files are uploaded as a GitHub Actions artifact named
`task-prompt-<TASK-ID>`.

The workflow does not modify the issue, create branches, open pull requests,
call Codex, or require secrets.

Ready Task Dispatch Discovery can trigger this generator automatically when a
task issue receives the `status:ready` label. Discovery remains read-only and
does not mutate labels, so prompt generation does not create a label loop.

## Required Issue Fields

- `Task ID`
- `Scope`

If either field is missing, the workflow fails clearly rather than generating a
vague prompt.

Optional fields are included as `not specified` when absent:

- `Lane`
- `Status`
- `Agent`
- `Depends on`
- `Unblocks`
- `Non-goals`
- `Allowed files`
- `Forbidden changes`
- `Acceptance criteria`
- `Required final report`

## Validation Inference

When a task issue does not specify validation commands, the generator infers a
validation section from the lane:

- `python`: root hygiene checks plus Python validation under `src/python`
- `dotnet`: root hygiene checks plus .NET validation under `src/dotnet`
- `ops` or `docs`: YAML, Bash, and repository hygiene checks
- any other lane: repository hygiene checks

## Guardrails

Generated handoff files are ignored by git through `.agent-handoff/`. Prompt
artifacts are intended for workflow artifacts and downstream execution, not for
repository commits.
