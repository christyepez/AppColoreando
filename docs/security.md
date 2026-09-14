# Security Notes

- Secrets are read from environment variables or local secret stores, not committed.
- Access tokens are short-lived JWTs.
- Refresh tokens are random, stored hashed, rotated on refresh and revocable per-session or all-sessions.
- Roles are enforced server-side: `Admin`, `ContentManager`, `Analyst`, `User`.
- Admin write endpoints require content/admin policies.
- Rate limiting, CORS allow-listing and basic security headers are configured at the API edge.
- Technical audit, functional history and metrics are stored separately.


## Dependency-security baseline

Release validation includes `dotnet list package --vulnerable --include-transitive` and `npm audit --omit=dev`.

As of 2026-09-08:

- NuGet: no vulnerable direct/transitive packages reported by configured sources.
- Angular production dependency audit: 0 vulnerabilities.
- Angular development toolchain reports moderate advisories through `webpack-dev-server` (`qs`, `sockjs`/`uuid`). These packages are not shipped in the production browser bundle; the findings remain tracked until upstream toolchain fixes are available.
- CI blocks production Angular dependency vulnerabilities at moderate severity or higher.
