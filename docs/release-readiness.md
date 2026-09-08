# Release Readiness

## Local service endpoints

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`
- Prometheus metrics: `http://localhost:8080/metrics`
- Admin: `http://localhost:4200`
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
- Android release build no longer falls back to the debug signing key.

## Current validation environment

Validated on Windows with Flutter `3.47.2`, Dart `3.13.2` and .NET `10`.

Known external blockers on this workstation:

- Android SDK is not installed, so APK/AAB generation cannot be truthfully marked as executed here.
- iOS archive/signing requires macOS, Xcode and Apple signing credentials.
- Google Play and App Store production signing credentials are intentionally not stored in the repository.
- Flutter doctor also reports a Windows desktop Visual Studio detection issue caused by the missing `%PROGRAMFILES(X86)%` environment variable; Windows desktop is not a release target for this project.

## Required secret handling

Create `.env` from `.env.example` and use strong local values. Production values belong in the target secret store/CI environment, never Git.

Required runtime secrets include PostgreSQL password, JWT signing key, RabbitMQ password, MinIO credentials and any opt-in seed administrator password.
