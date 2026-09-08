# Security Notes

- Secrets are read from environment variables or local secret stores, not committed.
- Access tokens are short-lived JWTs.
- Refresh tokens are random, stored hashed, rotated on refresh and revocable per-session or all-sessions.
- Roles are enforced server-side: `Admin`, `ContentManager`, `Analyst`, `User`.
- Admin write endpoints require content/admin policies.
- Rate limiting, CORS allow-listing and basic security headers are configured at the API edge.
- Technical audit, functional history and metrics are stored separately.

