# Sprint Status

| Sprint | Status | Validation summary |
|---|---|---|
| S00 | Completed | Solution, Clean Architecture baseline, CI, Docker and architecture tests. |
| S01 | Completed | Flutter shell/design system; Android/iOS scaffold; Flutter validation passed. |
| S02 | Completed | .NET 10 API, PostgreSQL, auth, migrations, seed, health and ProblemDetails. |
| S03 | Completed | Categories, countries, collections, artwork/assets, brands and licensing model. |
| S04 | Completed | Deterministic artwork bundle and mobile coloring engine. |
| S05 | Completed | Zoom/pan, selection, hints, wrong-color feedback and completed styling. |
| S06 | Completed | Revision/idempotency progress sync client aligned with backend sync contract. |
| S07 | Completed | Persistent offline queue with deduplication/latest-state behavior. |
| S08 | Completed | API-backed catalog search/filter/paging and mobile catalog integration. |
| S09 | Completed | Hardened bundle validation and worker pipeline; .NET tests passed. |
| S10 | Completed | Angular admin login/guard/interceptor plus content/users/licenses/audit/metrics views; production build passed. |
| S11 | Completed | Centralized achievements/XP rules and tests. |
| S12 | Completed | Refresh-token rotation/replay/device-mismatch hardening and security tests. |
| S13 | Completed | Prometheus metrics, `/metrics`, unified backend/admin CI and observability stack. |
| S14 | Release-ready with external distribution blockers | IDs/version/signing guardrails/store checklist/mobile CI complete. Android SDK and Apple/Google production signing remain external prerequisites. |
| S15 | Completed | Flutter Web/Windows targets, Android emulator automation and Android/Web smoke validation. |
| S16 | Completed | Organic mobile artwork UX plus Content Generation foundation: source assets, presets, jobs, RabbitMQ worker, Visual Processor contract/stub, EF migration and 17/17 backend tests. |
| S17 | Completed | Visual Processing MVP: deterministic LAB segmentation, connected playable regions, contour cleanup, Bézier SVG paths, vivid source-derived palette, numbered line-art/colored previews, QA metrics, Docker/pytest validation and CI integration. |
| S18 | Completed | Palette Intelligence & Region Quality: Delta-E near-color merging, vivid named source-derived colors, edge-aware label smoothing, difficulty-aware micro-region merging and QA metrics; Kids/Detailed smoke comparison validated. |
| S19 | Completed | Difficulty Generator creates Kids/Easy/Normal/Detailed/Master variant packs from one source asset with a schema 2.1 root manifest and per-variant QA. |
| S20 | Completed | Semantic region candidates and roles added to generated bundles with semantic QA counts and heuristic beak/eye/water/sky/foliage detection. |
| S21 | Completed | Number placement/readability metadata added with safe radius, zoom threshold, dynamic font sizing and QA counts for hidden labels. |
| S22 | Completed | Deterministic WebP preview renderer emits 512 thumbnail, 768 catalog preview and 1024 line-art preview per generated variant. |
| S23 | Completed | Special Effects Engine: Aura, Tesoro, Revela, Postal Viva, Lumina and Eclipse alter palette/preview deterministically; 10/10 processor tests pass. |

## S24 Admin Generation Studio

S24 adds an authenticated Angular Generation Studio for source upload, source/preset/difficulty selection, generation-job creation, recent-job monitoring and generated-preview inspection. The Studio polls active jobs and renders catalog, line-art and special-effect assets through authenticated Blob URLs.

The backend exposes a constrained generation-artifact endpoint. Artifact kinds are whitelisted and resolved through the primary variant manifest; filesystem paths are canonicalized and must remain inside the generation-job root. Traversal/out-of-root access is rejected.

Validation: .NET Release build 0 warnings/0 errors; backend 20/20 tests pass; Angular production build passes; git diff check passes.

## S25 — Manual Fine Tuning — Complete
- Non-destructive `adjustments.json` overlay per generation job.
- Region overrides: color, semantic tag/role, number visibility and reviewer note.
- Generated bundle remains immutable; overrides are auditable and reversible.
- Generation Studio loads real generated regions/palette and supports save/edit/delete overrides.
- Artifact whitelist extended safely for `regions` and `palette`.
- Canonical-path validation blocks external primary manifests and artifact traversal.
- Validation: .NET Release build 0 warnings/0 errors; backend 23/23 tests PASS; Angular production build PASS.

## S26 — Mobile Generated Bundle Integration — Complete
- Visual Processor emits self-contained playable `bundle.json` schema 2.2 with palette, regions, SVG paths, metadata and adjustments overlay slot.
- Generation jobs can be published as immutable `Artwork` records with SHA-256 checksum and public API bundle/thumbnail endpoints.
- S25 fine-tuning overrides are materialized into a published copy; the original generation bundle remains unchanged.
- Flutter loads published artwork metadata, follows `assetUrl`, parses SVG/Bézier paths, applies overrides and reuses the existing coloring/hit-test engine.
- Demo artworks remain a safe fallback when generated content is unavailable.
- RabbitMQ publisher/consumer now share a case-stable `GenerationQueueMessage` contract.
- Docker shared content volume is initialized for non-root UID 1654 before API/Worker/Processor start.
- Local Docker environment `appcoloreando-dev` validated with API/Admin/Web/Processor/RabbitMQ/MinIO/Prometheus/Grafana endpoints healthy.
- End-to-end smoke PASS: login → upload → queue → worker → processor → PreviewReady → publish → public bundle + thumbnail.
- Validation: .NET Release 0 warnings/0 errors; backend 25/25 tests PASS; Flutter analyze PASS; Flutter 9/9 tests PASS; Flutter Web Release PASS; Visual Processor 10/10 PASS; `git diff --check` PASS.

## S27 — Mobile Runtime Performance — Complete
- `DemoRegion` caches normalized and scaled `Path` geometry, avoiding repeated SVG/path reconstruction during hit-test and repaint.
- `ArtworkSpatialIndex` partitions normalized artwork space and narrows hit-test candidates before precise `Path.contains` checks.
- Coloring repaints now use an explicit `paintRevision`, fixing same-length completed-set mutation edge cases while avoiding unnecessary redraws.
- Existing rendering and S26 generated-bundle contracts remain unchanged.
- Stress validation covers an artwork with 500 regions and verifies spatial candidate reduction plus path-cache reuse.
- Validation: Flutter analyze PASS; Flutter 11/11 tests PASS; S26 GitHub AppColoreando CI and Mobile CI both SUCCESS.

## S28 — Automatic QA — Complete

- Visual Processor emits a deterministic QA score from 0 to 100, a `publishable` flag, and structured quality issues.
- QA checks cover playable coverage, palette separation, micro-regions, and number readability.
- QA metadata is embedded in both the generated manifest and playable `bundle.json`.
- Publication requires `qa.publishable=true` and `qa.score>=90`; missing legacy QA or failing scores are rejected.
- Publication-store tests cover accepted and rejected QA thresholds.
- Docker shared-content permissions are initialized for the non-root runtime before API/Worker/Processor startup.
- Validation: Visual Processor 10/10 tests PASS; backend Release 0 warnings/0 errors; backend 26/26 tests PASS.

## S29 — AI Assisted Generation — Complete
- Optional provider-neutral semantic hints use normalized coordinates and apply across all generated difficulty variants.
- High-confidence hints (>= 0.75) can override S20 heuristic semantics; low-confidence hints are ignored.
- Every region records `semanticSource` and `semanticConfidence` for traceability.
- No external AI/model dependency is required; deterministic local generation remains the fallback.
- Visual Processor API validates hint coordinates, roles, tags, confidence and provider metadata.
- Validation: Visual Processor 12/12 tests PASS; .NET Release build 0 warnings/0 errors; backend 26/26 tests PASS; `git diff --check` PASS.
## S30 — Natural Spatial Segmentation — Complete
- LAB clustering now includes normalized XY spatial features to preserve local object coherence in photographic sources.
- Spatial compactness scales by difficulty: Kids/Easy favor larger organic regions while Detailed/Master preserve more local detail.
- Palette centers are recomputed from original LAB pixels after spatial clustering, so color fidelity is not contaminated by XY features.
- Equal colors in distant objects may form independent playable regions, reducing fragmented cross-image blobs.
- The algorithm remains deterministic and local; no new external model or service dependency is introduced.
- Visual Processor health engine updated to `s30-natural-spatial-segmentation`.
- Validation: Visual Processor 14/14 tests PASS.
## S31 — Subject-Aware Segmentation — Complete
- Foreground likelihood combines border-color distance with a conservative center prior.
- Subject likelihood participates in clustering so main objects are less likely to dissolve into background-color regions.
- Edge strength is included as a clustering feature to better preserve structural boundaries.

## S32 — Edge and Contour Intelligence — Complete
- Component masks are cleaned with difficulty-aware elliptical morphology before vectorization.
- Contour simplification adapts to compactness, preserving more geometry on irregular shapes and smoothing regular regions.
- SVG generation remains deterministic and compatible with the existing mobile bundle contract.

## S33 — Region Topology Guardrails — Complete
- Thin/sliver playable regions are detected using area ratio, aspect ratio and contour compactness.
- Non-playable slivers are excluded before bundle emission, reducing accidental taps and unreadable numbered zones.
- Validation: Visual Processor 16/16 tests PASS.
## S34 — Smart Palette Harmonization — Complete
- Difficulty-aware saturation floors keep generated palettes vivid while preserving dark/light structure.
- Harmonization runs before artistic style effects so presets remain predictable.

## S35 — Number Placement V2 — Complete
- Region labels now expose `labelPriority` based on safe-radius, area and center weighting.
- Existing zoom/readability metadata remains backward compatible.

## S36 — Photo to Illustration Preprocessing — Complete
- Difficulty-aware bilateral/median stylization reduces photographic noise before segmentation.
- Kids/Easy receive stronger abstraction while Detailed/Master retain more texture.

## S37 — Automatic Artistic Style Routing — Complete
- `presetCode=auto` deterministically selects Natural, Aura, Postal Viva or Lumina from image brightness, saturation and warmth.
- Explicit presets still override automatic routing.

## S38 — Generation Quality Scoring V2 — Complete
- QA adds contour compactness, low-compactness region count, playable-label ratio and subject-region ratio.
- Excessive contour complexity contributes to publishability scoring.

## S39 — Automatic Repair Engine — Complete
- Non-publishable variants automatically receive one deterministic repair attempt.
- Repair adapts color count, region target, simplification and edge sensitivity according to QA issue codes.
- A repair replaces the original only when QA score improves; initial score and repair metadata remain visible in the variant manifest.
- Validation: Visual Processor 20/20 tests PASS.