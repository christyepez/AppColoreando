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

## Mobile

Commands:

```text
flutter pub get
flutter analyze
flutter test
flutter build appbundle --release
flutter build ios
```

Results:

- `flutter analyze`: PASS, no issues.
- `flutter test`: PASS, 8 tests.
- Android AAB: NOT EXECUTED to completion because Android SDK is not installed on this workstation.
- iOS build: NOT AVAILABLE on this Windows host; Flutter does not expose the iOS build subcommand here.
- Flutter doctor confirms Flutter 3.47.2 / Dart 3.13.2 and reports the Android SDK as missing.

## Admin, dependencies and infrastructure

- Angular production build: PASS, 314.45 kB initial raw bundle / 84.73 kB estimated transfer.
- `npm audit --omit=dev --audit-level=moderate`: PASS, 0 production vulnerabilities.
- Full npm audit reports 7 moderate development-toolchain findings in `webpack-dev-server` / `sockjs` / `uuid` / `qs`; runtime dependencies are unaffected.
- `dotnet list ... package --vulnerable --include-transitive`: PASS, no vulnerable NuGet packages reported.
- `docker compose --env-file .env.example config --quiet`: PASS.
- Release identity guard: PASS (`com.appcoloreando.mobile`, no debug release signing).
- Tracked-secret pattern scan: PASS.
- API `/metrics`: previously validated with Prometheus HTTP/process/GC metrics.

## External release prerequisites

Production Android signing, Android SDK, Apple Developer/App Store Connect credentials, macOS/Xcode, published privacy/support URLs and licensed-IP rights evidence remain external deployment prerequisites and are intentionally not committed.
