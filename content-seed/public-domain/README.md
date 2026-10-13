# Public-domain starter collection

This folder stores **metadata only** for the local starter image collection. Binary photos are intentionally ignored from Git.

The current manifest contains 16 source photographs across eight categories. Each item records:
- local relative filename
- author/source
- license (CC0 or Public Domain)
- Wikimedia Commons source page

Run `python tools/content/import_public_domain_seed.py` from the repository root after:
1. downloading the local source images listed in `manifest.json`,
2. starting the local Development stack,
3. enabling `SEED__ENABLEDEVADMINLOGIN=true`.

The importer creates/updates categories and collections and publishes Kids, Normal and Master versions of each source photo. It is idempotent by published title.
