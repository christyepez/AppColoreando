# AppColoreando

Plataforma de coloreado por números con aplicación móvil, portal administrativo y backend centralizado.

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
- Catálogo administrable de ilustraciones, categorías, colecciones y licencias
- Contenido personalizado por usuario
- Progreso de coloreado y sincronización
- Favoritos, historial y actividad
- Métricas de uso por usuario
- Auditoría técnica y funcional
- Portal administrativo
- API documentada con OpenAPI
- Preparación para Android/iOS

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

> Las franquicias comerciales (Disney, Pixar, Paw Patrol, Bluey, Dragon Ball, Saint Seiya, Transformers, He-Man, etc.) deben cargarse únicamente cuando existan los derechos/licencias correspondientes. La plataforma modela esos derechos desde el inicio.

## Release and validation

- Sprint implementation status: `docs/sprint-status.md`
- Release readiness: `docs/release-readiness.md`
- Store checklist: `docs/store-release-checklist.md`
- Latest validation evidence: `docs/release-validation-2026-09-08.md`

The repository is release-ready at source/CI level. Native store artifacts remain gated by Android SDK/production signing and macOS/Xcode/Apple signing prerequisites documented above.
