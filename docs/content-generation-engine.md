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

## S19 next

S19 will formalize the Difficulty Generator: generate Kids, Easy, Normal, Detailed and Master variants from one source asset in a single job, persist variant manifests, compare QA across variants and prepare the catalog/mobile contracts for selecting a generated variant.
