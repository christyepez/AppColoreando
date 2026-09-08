# Release Readiness

## Local Ports

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`
- Admin: `http://localhost:4200`
- PostgreSQL: `localhost:5432`
- RabbitMQ management: `http://localhost:15672`
- MinIO console: `http://localhost:9001`

## Credentials

Create `.env` from `.env.example` and set strong local values. Demo admin creation is opt-in through:

```text
SEED__ADMINEMAIL=admin@appcoloreando.local
SEED__ADMINPASSWORD=<local strong password>
JWT_KEY=<at least 32 random characters>
```

## Store Placeholders

Android package ID: `com.appcoloreando.mobile`

iOS bundle ID: `com.appcoloreando.mobile`

Signing, Apple Developer, Google Play Console, privacy policy publication and licensed IP rights are external prerequisites.

## QA Smoke Test

1. Run `docker compose --env-file .env up --build`.
2. Open `http://localhost:8080/health`.
3. Open Swagger and authenticate through `/api/auth/login`.
4. Open `http://localhost:4200`, sign in with the seeded admin, inspect dashboard/users/content/audit.
5. Install Flutter locally, then run `flutter create . --platforms android,ios` inside `apps/mobile` if native folders are needed, followed by `flutter pub get`, `flutter analyze`, and `flutter test`.

