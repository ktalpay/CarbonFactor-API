# Task Workflow

CarbonOps-API uses a lightweight task issue model for agent-ready work. The
model is documentation-first for now: tasks are represented as GitHub issues,
agents work on feature branches, and pull requests target `develop` for user
review and merge.

## Workflow Overview

1. Create a task issue using the CarbonOps-API task template.
2. Assign a unique task ID, lane, status, agent, dependencies, and acceptance
   criteria.
3. Move tasks through status labels as work becomes ready, starts, enters
   review, needs fixes, or merges.
4. Have Codex or another agent create a task branch and open a pull request into
   `develop`.
5. Keep merging as a user-owned review step.

Current automation supports merged-task watching, ready-task discovery, and
prompt handoff generation. It does not create labels, create branches, open
pull requests, run Codex, or merge work.

## Documentation

- [Task Labels](task-labels.md)
- [Task Issue Model](task-issue-model.md)
- [Task Prompt Handoff](task-prompt-handoff.md)
