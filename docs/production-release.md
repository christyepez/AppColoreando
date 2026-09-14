# AppColoreando Production Release 1.0.0

Release candidate branch: `release/rc-1-20260914`
Mobile version: `1.0.0+1`
RC baseline commit: `4f914b2`

## Pre-release gates
- Backend: all .NET tests green.
- Flutter: analyze/test/web release green.
- Visual processor: pytest green in container.
- Angular admin: production build green.
- Android: AAB generated on MarketingIndo.
- High-risk secret scan: zero matches.
- Production configuration fails closed on unsafe defaults.

## Production variables
- `ConnectionStrings__Postgres`
- `Jwt__Key` (minimum 32 chars, secret store only)
- `Jwt__Issuer`
- `Jwt__Audience`
- `Cors__AllowedOrigins__0..N`
- `AllowedHosts`
- `RabbitMQ__Host`, `RabbitMQ__User`, `RabbitMQ__Password`
- Content-generation storage paths/credentials as applicable
- Docker Hub credentials remain GitHub secrets, never committed
## Release sequence
1. Merge the approved RC into the protected production branch.
2. Create annotated tag `v1.0.0` from the approved merge commit.
3. Let Docker Publish build immutable version + SHA tags.
4. Deploy API/Worker/Visual/Admin/Web using the exact version tags.
5. Apply database migrations in a controlled deployment step.
6. Run `/health`, authentication, catalog, coloring and admin smoke tests.
7. Upload signed Android AAB and signed iOS archive through store accounts.
8. Verify store metadata, privacy declarations and phased/staged rollout.

## Rollback
- Keep the previous production image digests/tags available.
- Roll application containers back before attempting database reversal.
- Database migrations must be forward-compatible; do not auto-downgrade schema.
- Disable new content/features through feature gates when rollback is safer than redeploy.
- Re-run health and smoke tests after rollback.

## Manual approvals still required
- Merge to production/main.
- Creation/push of production tag `v1.0.0`.
- Production secrets/domains/endpoints.
- Apple/Google signing credentials and store product identifiers.
- Actual deployment and store submission/publishing.

## Final validation snapshot
- Backend Release build with `--warnaserror`: 0 warnings / 0 errors.
- Flutter analyze: 0 issues.
- Flutter tests: 22/22 passed.
- Admin production dependency audit: 0 vulnerabilities.
- Flutter Web release: built successfully.
- Android AAB: built successfully, 49.9 MB.
- Visual processor: 25/25 tests passed in Docker.
- Admin production build: successful.
- Known non-blocking toolchain warning: build expects CupertinoIcons font although application source does not reference CupertinoIcons.
