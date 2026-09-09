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

## S17 next

S17 replaces the foundation processor stub with real preprocessing, segmentation, contour extraction, region cleanup, vectorization and first playable bundle generation. The first acceptance target is an uploaded animal/flower image converted automatically into closed organic vector regions with an image-derived vivid palette.
