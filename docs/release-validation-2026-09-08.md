# Release Validation — 2026-09-08

## Backend

Command set:

```text
dotnet restore backend/AppColoreando.slnx
dotnet build backend/AppColoreando.slnx -c Release --no-restore --warnaserror
dotnet test backend/AppColoreando.slnx -c Release --no-build
```

Result: PASS.

- Build: 0 warnings, 0 errors.
- Application tests: 8 passed.
- Domain tests: 2 passed.
- Integration tests: 1 passed.
- Architecture tests: 3 passed.
- Total .NET tests: 14 passed.

## Mobile / Multi-platform

Commands include:

```text
flutter analyze
flutter test
flutter build web --release
flutter build apk --debug
flutter build appbundle --release
```

Results:

- `flutter analyze`: PASS, no issues.
- `flutter test`: PASS, 8 tests.
- Flutter Web release build: PASS.
- Android debug APK build: PASS.
- Android emulator install/launch smoke test: PASS on Android 16 / API 36 `emulator-5554`.
- Android release AAB compilation: PASS, `app-release.aab` generated at 49.1 MB.
- Android application ID / MainActivity package: PASS, `com.appcoloreando.mobile`.
- iOS build: NOT AVAILABLE on this Windows host; requires supported macOS/Xcode.

## Admin, dependencies and infrastructure

- Angular production build: PASS, 314.45 kB initial raw bundle / 84.73 kB estimated transfer.
- `npm audit --omit=dev --audit-level=moderate`: PASS, 0 production vulnerabilities.
- Full npm audit findings remain development-toolchain only; runtime dependencies are unaffected.
- `dotnet list ... package --vulnerable --include-transitive`: PASS, no vulnerable NuGet packages reported.
- `docker compose config --quiet`: PASS.
- Release identity guard: PASS (`com.appcoloreando.mobile`, no debug release signing).
- Tracked-secret pattern scan: PASS; only environment-variable placeholders were detected.
- API `/health`: PASS locally in Docker.
- API `/metrics`: previously validated with Prometheus HTTP/process/GC metrics.

## External release prerequisites

Production Android signing credentials / Google Play access, Apple Developer/App Store Connect credentials, supported macOS/Xcode, published privacy/support URLs and licensed-IP rights evidence remain external deployment prerequisites and are intentionally not committed.
