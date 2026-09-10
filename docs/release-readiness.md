# Release Readiness

## Local service endpoints

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`
- Prometheus metrics: `http://localhost:8080/metrics`
- Admin: `http://localhost:4200`
- User Web/PWA: `http://localhost:8083`
- PostgreSQL: `localhost:5432`
- RabbitMQ management: `http://localhost:15672`
- MinIO console: `http://localhost:9001`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

## Mobile release identity

- Product: `AppColoreando`
- Version: `1.0.0+1`
- Android application ID: `com.appcoloreando.mobile`
- iOS bundle ID: `com.appcoloreando.mobile`
- Android release build never falls back to the debug signing key.

## Current validation environment

Validated on Windows with Flutter `3.47.2`, Dart `3.13.2`, .NET `10`, Android SDK 36 and NDK `28.2.13676358`.

Validated locally:

- Android 16 / API 36 `medium_phone` AVD created and detected by Flutter.
- Debug APK built, installed and launched successfully on `emulator-5554`.
- Android release AAB compilation passed; `app-release.aab` generated at 49.1 MB.
- `MainActivity` remained foreground with no fatal Android runtime exception.
- Flutter Web Release build completed successfully.
- Web/PWA and Android use the same Flutter codebase with environment-specific API endpoints.

Remaining external blockers:

- iOS archive/signing requires supported macOS, Xcode and Apple signing credentials.
- Google Play and App Store production signing credentials are intentionally not stored in the repository.
- Windows native `.exe` requires Windows Developer Mode and the Visual Studio Desktop development with C++ workload on this workstation.

## Required secret handling

Create `.env` from `.env.example` and use strong local values. Production values belong in the target secret store/CI environment, never Git.

Required runtime secrets include PostgreSQL password, JWT signing key, RabbitMQ password, MinIO credentials and any opt-in seed administrator password.
