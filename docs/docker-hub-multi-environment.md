# Docker Hub multi-environment deployment

S30 standardizes `trabajo` and `MarketingIndo` on the same immutable application images.
Each workstation keeps its own local PostgreSQL/MinIO/content volumes and secrets, while application binaries come from Docker Hub.

## Images

A single `APPCOLOREANDO_VERSION` selects all application components:

- `christyepez/appcoloreando-api`
- `christyepez/appcoloreando-worker`
- `christyepez/appcoloreando-visual-processor`
- `christyepez/appcoloreando-admin`
- `christyepez/appcoloreando-web`

Public infrastructure images remain PostgreSQL 17, Redis 7, RabbitMQ 4, MinIO and Alpine.
Never place passwords or tokens in the repository.
## Workstation setup

1. Copy `.env.hub.example` to `.env.hub` and replace all placeholder secrets.
2. Authenticate Docker Desktop/CLI if the application repositories are private.
3. Choose the same `APPCOLOREANDO_VERSION` on both workstations.
4. Start with `tools\docker-hub\hub-up.ps1 -ProjectName appcoloreando-trabajo` or `appcoloreando-marketingindo`.
5. Stop with `tools\docker-hub\hub-down.ps1`; volumes are preserved.

The start script performs `docker compose pull`, starts the complete application stack and waits for API and Visual Processor health checks.
Admin is exposed on port 4200, Flutter Web on 8083, API on 8080 and the Visual Processor on 8090 by default.

## Publishing

`.github/workflows/docker-publish.yml` builds all five application images from one Git commit and tags every image with the same release tag plus `sha-<12 chars>`.
Publishing requires repository secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`.
Do not use `latest` for reproducible multi-workstation development; use a release or SHA tag.