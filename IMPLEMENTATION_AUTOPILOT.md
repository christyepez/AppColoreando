# AppColoreando Full Implementation Autopilot

You are implementing the complete AppColoreando product in this repository. Do not ask the user for intermediate decisions. Make reasonable engineering decisions, document them, and continue automatically until the full planned implementation is complete or a genuine external blocker makes a requirement impossible.

## Mandatory governance

Read and obey these shared standards from the sibling repository before changing code:
- `C:\Users\ChristianYepez\source\repos\CodexCommonAgents\AGENTS.md`
- `C:\Users\ChristianYepez\source\repos\CodexCommonAgents\agents\03-backend-agent.md`
- `C:\Users\ChristianYepez\source\repos\CodexCommonAgents\rules\00-global-rules.md`
- `C:\Users\ChristianYepez\source\repos\CodexCommonAgents\rules\01-backend-clean-architecture.md`

The backend architecture contract is mandatory:
`API -> Application -> Domain` and `Infrastructure -> Application + Domain`.
Controllers must be thin. No business logic or DbContext in controllers. `Program.cs` is composition root only.

## Product goal

Build a production-oriented color-by-number platform inspired functionally by coloring apps, but with its own implementation, UX and assets:
1. Flutter mobile app for Android/iOS.
2. Angular admin portal.
3. ASP.NET Core .NET 10 backend.
4. PostgreSQL database initially, designed so SQL Server can be supported later without domain changes.
5. Docker Compose local stack.
6. User accounts, authentication, authorization, profile, preferences and user-owned content/progress.
7. Audit, functional history and usage metrics.
8. Content management including categories, collections, countries, artwork, publishing workflow, assets and licensing rights.
9. Offline-first mobile coloring experience.
10. Tests, CI/CD, docs, security and observability.

## IP/legal constraint

Do NOT add copyrighted character artwork or unlicensed assets from Disney, Pixar, Paw Patrol, Bluey, Dragon Ball, Saint Seiya, Transformers, He-Man, ThunderCats, SilverHawks, Mazinger Z or other third-party franchises. Architecture may model licensed brands/rights and seed brand placeholders with `Enabled=false`, but sample artwork must be original, generic, public-domain-safe or generated geometric/demo content.

## Technology baseline

- Backend: .NET 10, ASP.NET Core 10, EF Core 10, PostgreSQL/Npgsql.
- Mobile: Flutter/Dart, Riverpod, GoRouter, Drift/SQLite where appropriate.
- Admin: Angular standalone architecture.
- Local infra: Docker Compose, PostgreSQL, Redis, RabbitMQ, MinIO, Prometheus, Grafana where practical.
- API: REST/OpenAPI, ProblemDetails.
- Auth: JWT access tokens + refresh-token rotation/revocation.
- Tests: xUnit, integration tests, architecture tests, Flutter tests, Angular tests when tooling supports it.
- CI: GitHub Actions.
- Observability: structured logging and OpenTelemetry-ready hooks.

## Git execution rules

Current branch is `sprints/S00-S03-foundation-domain` based on main. Preserve existing work. Implement sprint-by-sprint and commit each completed sprint with a clear message, e.g. `S00: ...`, `S01: ...`.
Do NOT push until local validation for that sprint passes. When a sprint cannot be fully validated because a required tool is absent, document `NOT EXECUTED` and continue with all feasible work.
Do not rewrite history or force-push.

## Sprint roadmap

### S00 - Architecture, repository, CI/CD and Docker
- Complete solution file and project organization.
- Add `.gitignore`, Directory.Build.props, optional Directory.Packages.props if useful.
- Add domain/application/infrastructure/api test projects.
- Add architecture tests enforcing dependency rules.
- Add local Docker Compose services: API, PostgreSQL, Redis, RabbitMQ, MinIO, admin; Prometheus/Grafana if reasonable.
- Add health endpoints/config and environment templates.
- Add architecture docs, ADRs, C4 textual/mermaid diagrams and sprint/status docs.
- CI builds backend and admin; mobile CI can be added once Flutter scaffold is complete.

### S01 - Flutter shell and design system
- Ensure a real Flutter project scaffold exists, including android/ios folders if Flutter tooling is available.
- Implement app shell, GoRouter, Riverpod, theme/design tokens, localization skeleton (ES/EN), bottom navigation, home/catalog/profile/settings routes.
- Add API configuration and environment handling.
- Add original placeholder/demo artwork presentation only.

### S02 - .NET API and PostgreSQL foundation
- Replace `EnsureCreated` with proper EF Core migrations and controlled startup migration behavior.
- Add seed system that is environment-safe and idempotent.
- Add robust configuration, ProblemDetails, validation, correlation, health checks and OpenAPI.
- Add JWT access + refresh tokens with rotation/revocation and secure password hashing.
- Add roles: Admin, ContentManager, Analyst, User. Permission checks server-side.
- Add user profile/preferences endpoints and admin user management.

### S03 - Artwork/category/collection/licensing data model
- Add entities and contracts for Category, Collection, CollectionArtwork, Artwork, ArtworkAsset, Brand, LicenseAgreement, LicenseTerritory/rights, publishing status.
- Licensing must support territory, valid-from/to, store distribution allowance and feature enablement.
- Add CRUD/query services and admin endpoints with audit.
- Add catalog filtering/search by country/category/collection/difficulty/license status.

### S04 - Coloring Engine v1
- Define artwork bundle schema: palette + regions + region IDs + expected color IDs + vector/path or polygon representation.
- Implement Flutter interactive coloring canvas with a deterministic bundled demo artwork (original geometry), color-number selection and hit detection.
- Persist progress locally.

### S05 - Zoom/pan/selection/regions
- Add zoom/pan, region highlighting, hints, wrong-color feedback, completed region styling and basic performance safeguards.
- Add unit/widget tests for coloring logic.

### S06 - Cloud persistence and progress
- Backend progress APIs must support idempotent sync, revision/version conflict handling and progress snapshots/events.
- User content endpoints for favorites, started/completed, recent and personal library.
- Mobile sync repository connects local progress to backend when authenticated.

### S07 - Offline-first
- Implement local catalog/artwork metadata cache and queued sync operations.
- Downloaded-artwork metadata and offline availability.
- Retry/backoff/conflict policy.
- User can color downloaded demo content without network.

### S08 - Catalog + search
- Mobile catalog sections, country/collection/category browsing, search, filters, favorites and recently used.
- Backend search/filter APIs with pagination.
- Admin catalog management UI should be functional against API.

### S09 - Content processing pipeline
- Implement a safe deterministic content processor/worker for validating artwork bundle JSON/SVG metadata, palette/region consistency, checksums and difficulty calculation.
- Do not implement scraping/copyright ingestion.
- Add Worker project and RabbitMQ abstraction if useful; synchronous fallback acceptable for local development.
- Publishing workflow: Draft -> Processing -> QA -> Approved -> Scheduled -> Published -> Archived.

### S10 - Angular Admin
- Finish real admin app integration with API: login, dashboard, users, categories, collections, artwork, assets, licenses/rights, audit, metrics.
- Route guards, HTTP interceptor for bearer tokens, reusable forms/tables, error handling.
- No static-only fake pages for core modules.

### S11 - Collections and achievements
- Backend collection completion calculations, achievements, streaks and XP model.
- Seed original country collections for Ecuador, Colombia, South America and USA with safe demo metadata.
- Mobile achievements/profile progress UI.

### S12 - Authentication + cloud sync hardening
- Refresh-token rotation, logout/revoke-all, device/session metadata, account endpoints.
- Secure local token storage abstraction on mobile.
- Sync progress/history/preferences.
- Add authorization tests.

### S13 - Security, performance and observability
- Rate limiting, restrictive CORS configuration, security headers where relevant, validation, audit enrichment, structured logs, correlation IDs.
- OpenTelemetry configuration hooks.
- Docker non-root runtime.
- Dependency/container scanning workflows where feasible.
- Performance-oriented indexes and pagination.
- Architecture/security documentation.

### S14 - Store/release readiness
- Android/iOS metadata/config placeholders, application IDs, versioning strategy, privacy/terms placeholders and child/kids-mode compliance checklist.
- GitHub Actions workflows for build/test; store signing/publishing must remain placeholders unless credentials are available.
- Release checklist, QA test plan, Postman collection/environment, operator/admin guide, developer setup guide.
- Final Docker Compose validation and API/admin smoke tests if Docker is available.

## User-owned content requirements

The platform must clearly support per-user state:
- profile/preferences/kids mode;
- favorites;
- started, in-progress and completed artwork;
- progress and completed region IDs;
- history/activity timeline;
- achievements/streak/XP;
- sessions/devices where appropriate;
- daily metrics;
- sync metadata/revision;
- downloaded/offline state on device.

## Audit and metrics requirements

Keep separate:
- technical audit (entity changes / who / when / before-after where feasible);
- functional history (login, progress saved, artwork completed, favorite added, etc.);
- reporting metrics (daily counters and dashboard aggregates).

## Validation requirements

For backend, run at least:
- `dotnet restore backend/AppColoreando.slnx`
- `dotnet build backend/AppColoreando.slnx -c Release --warnaserror`
- `dotnet test backend/AppColoreando.slnx -c Release --no-build`

For admin:
- `npm ci` or `npm install`
- `npm run build`
- available tests/lint.

For mobile:
- if Flutter is available: `flutter pub get`, `flutter analyze`, `flutter test`, and at least an Android build if SDK/toolchain supports it.
- if Flutter is unavailable, create valid project source/config to the maximum feasible extent and document validation as NOT EXECUTED; do not pretend it passed.

## Completion behavior

Continue across all sprints automatically. Fix failures before moving on when reasonably possible. At the end, produce a concise final implementation report with:
- sprint status S00-S14;
- commits;
- validations and test counts;
- URLs/ports;
- demo/admin credential setup method without committing secrets;
- exact steps for the user to test mobile, admin and API;
- genuine remaining external blockers only (e.g. Apple/Google signing credentials or licensed IP rights).
