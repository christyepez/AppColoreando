# AGENTS.md - AppColoreando

## Common governance

This repository uses `christyepez/CodexCommonAgents` as its mandatory common governance baseline.

Before Docker, Docker Compose, image publishing, cleanup or multi-machine runtime work, Codex must read and apply:

```text
CodexCommonAgents/AGENTS.md
CodexCommonAgents/rules/02-docker-runtime-and-image-governance.md
CodexCommonAgents/playbooks/docker-multi-machine-runtime.md
```

## Project runtime rule

`trabajo` and `MarketingIndo` must use the same immutable project-owned image version when they represent the same AppColoreando environment.

Project-owned images must be published to the approved registry and pinned by immutable tag or digest for shared runtime. Local builds are allowed for development, but they must not become the shared multi-machine baseline by accident.

Database and persistent volumes must be preserved during image/container alignment unless explicitly authorized otherwise.

## Validation

Docker changes must finish with `docker compose config`, image/digest comparison and health/port verification.
