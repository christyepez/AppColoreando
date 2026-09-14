# S51 — Store Readiness

## Android
- Application ID: `com.appcoloreando.mobile` (verify ownership/availability before first Play Console registration).
- Target SDK pinned to API 36 for 2026 Google Play submissions.
- Release manifest explicitly includes INTERNET permission.
- Release signing remains external to source control and must be injected by CI/store tooling.
- Build/version source remains `pubspec.yaml` (`version: 1.0.0+1`).

## iOS
- Bundle ID remains `com.appcoloreando.mobile` until Apple account ownership is confirmed.
- Deployment target remains iOS 15.0.
- `PrivacyInfo.xcprivacy` is included in Runner resources.
- UserDefaults required-reason declaration uses CA92.1 for app-local preferences.
- Signing/team/provisioning profiles are intentionally not committed.
## Store metadata / privacy
- Prepare a public privacy-policy URL before production submission.
- Complete Google Play Data safety for account/profile, progress, favorites, telemetry and diagnostics actually collected by production builds.
- Complete App Store App Privacy answers for the same production behavior and third-party SDKs.
- Do not claim tracking: current first-party telemetry is allow-listed and designed without advertising identifiers.
- Screenshots, feature graphics, age/content rating and support/contact URLs remain store-account deliverables.

## External blockers
- Google Play signing key / Play App Signing enrollment.
- Apple Developer Team, certificates and provisioning profiles.
- Final ownership confirmation for package/bundle identifier.
- Real subscription product IDs and receipt validation when in-app purchases are activated.