# AppColoreando

Plataforma de coloreado por nÃºmeros con aplicaciÃ³n mÃ³vil, portal administrativo y backend centralizado.

## Stack
- Mobile: Flutter
- Admin: Angular
- Backend: ASP.NET Core .NET 10
- Database: PostgreSQL
- Cache: Redis
- Object storage: MinIO (local), compatible con S3/Blob en cloud
- Observability: OpenTelemetry + Prometheus/Grafana
- Containers: Docker Compose

## Core capabilities
- Registro/login de usuarios y roles
- CatÃ¡logo administrable de ilustraciones, categorÃ­as, colecciones y licencias
- Contenido personalizado por usuario
- Progreso de coloreado y sincronizaciÃ³n
- Favoritos, historial y actividad
- MÃ©tricas de uso por usuario
- AuditorÃ­a tÃ©cnica y funcional
- Portal administrativo
- API documentada con OpenAPI
- PreparaciÃ³n para Android/iOS

## Repository layout
- `backend/`: API, Application, Domain, Infrastructure y tests
- `apps/mobile/`: Flutter
- `apps/admin/`: Angular
- `infrastructure/`: Docker y observabilidad
- `docs/`: arquitectura, ADRs, seguridad y roadmap

## Local development

```bash
docker compose up --build
```

Servicios previstos:
- API: `http://localhost:8080`
- Admin: `http://localhost:4200`
- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`
- MinIO: `http://localhost:9001`

> Las franquicias comerciales (Disney, Pixar, Paw Patrol, Bluey, Dragon Ball, Saint Seiya, Transformers, He-Man, etc.) deben cargarse Ãºnicamente cuando existan los derechos/licencias correspondientes. La plataforma modela esos derechos desde el inicio.

## Release and validation

- Sprint implementation status: `docs/sprint-status.md`
- Release readiness: `docs/release-readiness.md`
- Store checklist: `docs/store-release-checklist.md`
- Latest validation evidence: `docs/release-validation-2026-09-08.md`

The repository is release-ready at source/CI level. Native store artifacts remain gated by Android SDK/production signing and macOS/Xcode/Apple signing prerequisites documented above.

## Multi-platform execution

The Flutter client targets Android, iOS, Web and optional Windows Desktop from the same codebase.

Local PC simulation:

```powershell
.\tools\simulation\run-app.ps1 -Target web
```

Web server profile:

```powershell
.\tools\simulation\run-web.ps1 -Port 8083
```

Android emulator setup and iOS/macOS requirements are documented in `docs/multiplatform-simulation.md`.
