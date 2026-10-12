# AppColoreando Privacy Policy

Effective date: 12 October 2026

AppColoreando is a color-by-number application. This policy describes the data handled by the application and its backend services.

## Data we handle

When an account is used, AppColoreando may process:
- account information such as email address, display name and locale;
- coloring progress, favorites, downloaded-artwork state and achievements;
- device/session identifiers used for authentication and session security;
- allow-listed product telemetry such as artwork opens, favorites, colored regions and completion events;
- technical diagnostics required to operate and secure the service.

AppColoreando does not intentionally collect advertising identifiers and does not sell personal data to advertisers.

## Why we use data

Data is used to provide authentication, synchronize progress, personalize recommendations, preserve favorites and achievements, operate the service, prevent abuse, troubleshoot failures and improve product reliability.

## Telemetry

First-party telemetry is restricted to an allow-list of event names and property keys. Free-form personally identifying telemetry fields are rejected by the backend. Production store disclosures must reflect the actual SDKs and features enabled in the production build.

## Storage and security

Authentication secrets are not stored in source control. Passwords are stored as password hashes. Refresh tokens are stored as hashes and can be revoked. Production secrets must be supplied through the deployment environment or secret store.

## Export and deletion

Authenticated end users can request a machine-readable account export through `GET /api/me/account/export`.

Authenticated end users can permanently delete their account through `DELETE /api/me/account` with explicit confirmation. Deletion removes the user record, profile, refresh tokens, progress, synchronization operations, achievements, activity/telemetry history and daily metrics. Historical audit records are dissociated from the deleted user.

Privileged editorial/administrative accounts cannot self-delete and must be handled by an administrator to protect operational and publishing integrity.

## Children and kids mode

AppColoreando includes a kids-mode preference, but production distribution must use the appropriate store age rating and family/kids disclosures for the features actually enabled. The service should not be represented as collecting data from children unless the production configuration and legal basis support that use.

## Retention

Account-linked data is retained while the account is active or as needed to provide the service. Account deletion removes the personal data described above. Operational records that no longer identify the deleted account may be retained for integrity, security and audit purposes.

## Third parties

Production disclosures must be updated if third-party analytics, advertising, payment, authentication or other data-processing SDKs are added. The current first-party telemetry design does not use advertising identifiers.

## Changes

This policy may be updated when AppColoreando features, data practices or production integrations change. The effective date above identifies the current version.

## Support

For product support and privacy questions, use the public support channel documented at:
https://github.com/christyepez/AppColoreando/blob/main/docs/support.md
