# Content Generation Engine

## Goal

AppColoreando converts authorized source artwork into playable color-by-number bundles through an original processing pipeline. The product may target a comparable level of usability and polish to leading coloring applications, but must not copy proprietary source code, assets, models, layouts or trade dress.

## S16 Foundation

The S16 foundation introduces:

- `SourceAsset` ingestion for JPEG, PNG and SVG.
- SHA-256 integrity metadata and a 25 MB upload guard.
- database-backed `StylePreset` definitions.
- `ArtworkGenerationJob` lifecycle and issue tracking.
- RabbitMQ queue `artwork.generation.requested`.
- .NET 10 background Worker.
- internal Visual Processor HTTP contract.
- shared content volume for source and generated assets.
- EF Core migration `ContentGenerationFoundation`.

## Built-in presets

Natural, Kids, Detailed, Aura, Tesoro, Revela, Postal Viva, Lumina and Eclipse are seeded as data. Presets control target region count, color count, simplification, edge sensitivity, curve smoothness, saturation, contrast and semantic merging.

## Runtime flow

1. Admin uploads a source asset.
2. API validates type/size, hashes it and persists metadata.
3. Admin selects a style preset and difficulty.
4. API creates a queued generation job and publishes its id to RabbitMQ.
5. Worker loads the job, source asset and preset from PostgreSQL.
6. Worker calls the Visual Processor through its internal HTTP contract.
7. Visual Processor writes the result manifest under `/content-data/generation-jobs/{jobId}`.
8. Worker stores processor ids/result paths and transitions the job to `PreviewReady` or `Failed`.

## Validation evidence

- API and Worker build with `-warnaserror`: zero warnings/errors.
- Backend tests: 17/17 pass.
- Architecture tests preserve Domain/Application dependency boundaries.
- Flutter analyze: no issues.
- Flutter tests: 8/8 pass.
- Flutter Web release build: pass.
- Visual Processor Docker build: pass.
- Visual Processor health and missing-source error contract: pass.

## S17 Visual Processing MVP

S17 replaces the processor stub with a deterministic first-generation image-to-template engine. The pipeline performs edge-preserving preprocessing, LAB color clustering, connected-region extraction, morphology cleanup, contour simplification, Chaikin smoothing and cubic Bezier SVG generation. Colors are derived from the source image and receive controlled saturation/contrast enhancement rather than random assignment.

The generated playable bundle contains `artwork.svg`, `artwork-lineart.svg`, `regions.json`, `palette.json`, `preview-colored.png`, `preview-lineart.png` and `manifest.json`. Region label anchors are computed with a distance transform so paint numbers remain inside playable areas. QA metadata currently includes region count, color count, playable coverage and average region area.

Validation uses a synthetic animal composition and verifies closed vector regions, cubic Bezier commands, image-derived palette values, numbered line-art, bundle completeness and coverage. The Visual Processor is now a first-class GitHub Actions job and Docker image.

## S18 Palette Intelligence & Region Quality

S18 adds perceptual palette and region cleanup. K-means clusters are merged with CIE Lab Delta-E thresholds that vary by difficulty, avoiding multiple paint numbers for colors the eye perceives as effectively the same. Palette colors remain derived from the source image, receive controlled vividness tuning and are emitted with friendly names such as Blanco Nube, Azul Laguna, Amarillo Sol, Naranja Mandarina, Ambar Dorado, Verde Bosque and Carbon.

Labels are smoothed only away from detected edges, preserving important object boundaries. Small connected components are merged into perceptually compatible neighboring regions using difficulty-specific thresholds: Kids/Easy simplify aggressively while Detailed/Master retain more local detail. QA now records minimum palette Delta-E, the applied micro-region threshold and difficulty.

The same synthetic animal source produced 4 regions in Kids and 6 in Detailed with 100% playable coverage, demonstrating that difficulty now changes topology rather than only metadata.

## S19 Difficulty Generator

S19 generates Kids, Easy, Normal, Detailed and Master variants from one source asset in a single processor job. Each variant receives its own schema 2.0 playable bundle while a schema 2.1 root manifest indexes all five variants, their QA metrics and the primary difficulty selected by the Generation Job.

Difficulty derivation adjusts target region count, color capacity, contour simplification and edge sensitivity. The .NET Visual Processor request enables variant generation by default without requiring another persistence migration.

## S20 Semantic Region Intelligence

S20 adds deterministic semantic candidates to every generated region. Geometry, location and source-derived color are used to classify broad roles such as background, subject, subject-detail and environment, with useful candidates including beak, eye, water, sky, foliage and dark detail. These tags improve future merge rules and QA while remaining explicitly heuristic until a vision model is introduced.

Generated `regions.json` records `semanticTag` and `semanticRole`; QA records semantic tag counts per variant. The semantic layer is designed so a future AI vision adapter can replace or enrich heuristics without changing the mobile bundle shape.

## S21 Number Placement & Readability

S21 measures the maximum inscribed radius around each label anchor produced by the distance transform. Every region now exposes `labelRadius`, `labelMinZoom`, `labelFontSize` and `labelVisibleAtBase`, allowing narrow regions to defer their paint number until the user zooms instead of rendering unreadable or overlapping labels.

Raster and SVG line-art previews use the same readability decision. QA records labels visible at base scale and labels requiring zoom so dense artwork can be rejected or tuned before publication.

## S22 Preview Renderer

S22 generates deterministic square preview assets for the mobile library and Admin review workflow. Every playable variant now emits a 512x512 `thumbnail.webp`, 768x768 `catalog-preview.webp` and 1024x1024 `lineart-preview.webp` in addition to the original PNG and SVG assets.

The renderer preserves aspect ratio, centers artwork on a neutral canvas, applies consistent margins and uses fixed WebP quality settings so cards remain visually stable across differently shaped source images.

## S23 next

S23 will turn Aura, Tesoro, Revela, Postal Viva, Lumina and Eclipse into real processing behaviors rather than labels, with palette/preview transformations recorded explicitly in variant metadata.

## S23 Special Effects Engine

The generation preset now changes the rendered result, not only catalog metadata. Aura increases vibrancy and glow; Tesoro sharpens premium detail; Revela creates a partial-reveal preview; Postal Viva applies a warm editorial treatment; Lumina boosts highlights; Eclipse darkens the composition while preserving saturated accents. Every generated variant publishes special-preview.webp plus effect metadata in its manifest, while the underlying numbered regions remain playable.
