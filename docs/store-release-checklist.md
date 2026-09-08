# Store Release Checklist

## Mobile identity

- Product name: `AppColoreando`
- Android application ID: `com.appcoloreando.mobile`
- iOS bundle ID: `com.appcoloreando.mobile`
- Release version baseline: `1.0.0+1`

## Android release prerequisites

- Install Android SDK and required platform/build-tools.
- Configure production keystore outside source control.
- Inject signing credentials through CI secret storage or local `key.properties`.
- Never reuse debug signing for release artifacts.
- Validate `flutter build appbundle --release` before Google Play upload.
- Validate target SDK requirements current at publication time.

## iOS release prerequisites

- Build on macOS with current supported Xcode.
- Configure Apple Developer Team and App Store Connect application.
- Store certificates/profiles in Apple/CI secure signing facilities.
- Validate archive and TestFlight upload before production review.

## Store content and compliance

- Publish privacy policy and support contact URLs.
- Prepare store descriptions, screenshots, icons and age-rating answers.
- Complete Google Play Data Safety and Apple App Privacy declarations from actual telemetry/data use.
- Confirm account deletion/export flows before store submission if account creation is enabled.
- Verify accessibility, localization and parental/kids-mode disclosures.

## Intellectual-property gate

- Original/public-domain/properly licensed-safe packs may ship.
- Branded packs remain disabled until license metadata is valid for territory, dates and store distribution rights.
- Disney, Pixar, Bluey, Paw Patrol, Dragon Ball, Saint Seiya, Transformers, He-Man and other protected brands must not ship without documented rights.
- Expired or invalid rights must prevent publication/unpublish affected content.

## Final technical gate

- `.NET 10` Release build and tests pass with warnings as errors.
- Flutter `analyze` and tests pass.
- Angular production build passes.
- Docker Compose configuration validates.
- API `/health` and `/metrics` respond.
- No production secrets, signing material or private keys are committed.
