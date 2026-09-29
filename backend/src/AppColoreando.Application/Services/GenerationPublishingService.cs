using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Services;

public sealed class GenerationPublishingService(
    IGenerationJobRepository jobs,
    ISourceAssetRepository sourceAssets,
    IGenerationAdjustmentStore adjustments,
    IGenerationPublicationStore publication,
    IArtworkRepository artworks,
    ICategoryRepository categories,
    ICollectionRepository collections,
    IAuditRepository audit,
    IUnitOfWork uow) : IGenerationPublishingService
{
    public async Task<ArtworkDto> PublishAsync(
        Guid userId,
        Guid jobId,
        PublishGenerationRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var job = await jobs.GetAsync(jobId, ct)
            ?? throw new KeyNotFoundException("Generation job not found.");
        EnsureApproved(job);

        var category = await GetActiveCategoryAsync(request.CategoryId, ct);
        var artwork = await MaterializeArtworkAsync(
            userId,
            job,
            request.Title.Trim(),
            category.Id,
            request.CountryCode,
            request.Description,
            ct);

        await uow.SaveChangesAsync(ct);
        return Map(artwork);
    }

    public async Task<GenerationBatchPublicationDto> PublishBatchAsync(
        Guid userId,
        Guid batchId,
        PublishGenerationBatchRequest request,
        CancellationToken ct)
    {
        var batchJobs = (await jobs.ListByBatchAsync(batchId, ct))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToArray();

        if (batchJobs.Length == 0)
            throw new KeyNotFoundException("Generation batch not found.");

        var category = await GetActiveCategoryAsync(request.CategoryId, ct);
        var collection = request.CollectionId.HasValue
            ? await GetActiveCollectionAsync(request.CollectionId.Value, ct)
            : null;

        foreach (var job in batchJobs)
            EnsureApproved(job);

        var assetsById = new Dictionary<Guid, SourceAsset>();
        foreach (var sourceAssetId in batchJobs.Select(x => x.SourceAssetId).Distinct())
        {
            var asset = await sourceAssets.GetAsync(sourceAssetId, ct)
                ?? throw new KeyNotFoundException($"Source asset not found: {sourceAssetId}.");
            assetsById[sourceAssetId] = asset;
        }

        var published = new List<Artwork>(batchJobs.Length);
        var nextSortOrder = collection?.Artworks.Count > 0
            ? collection.Artworks.Max(x => x.SortOrder) + 1
            : 0;
        var effectiveCountryCode = string.IsNullOrWhiteSpace(request.CountryCode)
            ? collection?.CountryCode
            : request.CountryCode;

        foreach (var job in batchJobs)
        {
            ct.ThrowIfCancellationRequested();
            var asset = assetsById[job.SourceAssetId];
            var title = BuildBatchTitle(request.TitlePrefix, asset.FileName);
            var artwork = await MaterializeArtworkAsync(
                userId,
                job,
                title,
                category.Id,
                effectiveCountryCode,
                request.Description,
                ct);
            published.Add(artwork);

            if (collection is not null)
            {
                collection.Artworks.Add(new CollectionArtwork
                {
                    CollectionId = collection.Id,
                    ArtworkId = artwork.Id,
                    SortOrder = nextSortOrder++
                });
            }
        }

        audit.Add(new AuditLog
        {
            UserId = userId,
            EntityName = nameof(ArtworkGenerationJob),
            EntityId = batchId.ToString(),
            Action = "GenerationBatchPublished",
            ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                batchId,
                count = published.Count,
                collectionId = collection?.Id,
                artworkIds = published.Select(x => x.Id).ToArray()
            })
        });

        await uow.SaveChangesAsync(ct);

        return new GenerationBatchPublicationDto(
            batchId,
            published.Count,
            published.Select(Map).ToArray());
    }

    private async Task<Category> GetActiveCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        var category = await categories.GetAsync(categoryId, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        if (!category.IsActive)
            throw new InvalidOperationException("Category is inactive.");
        return category;
    }

    private async Task<Collection> GetActiveCollectionAsync(Guid collectionId, CancellationToken ct)
    {
        var collection = await collections.GetAsync(collectionId, ct)
            ?? throw new KeyNotFoundException("Collection not found.");
        if (!collection.IsActive)
            throw new InvalidOperationException("Collection is inactive.");
        return collection;
    }

    private async Task<Artwork> MaterializeArtworkAsync(
        Guid userId,
        ArtworkGenerationJob job,
        string title,
        Guid categoryId,
        string? countryCode,
        string? description,
        CancellationToken ct)
    {
        var overlay = await adjustments.ListAsync(job.ResultManifestPath!, ct);
        var artworkId = Guid.NewGuid();
        var materialized = await publication.MaterializeAsync(
            artworkId, job.ResultManifestPath!, overlay, ct)
            ?? throw new InvalidOperationException("Generation bundle could not be materialized.");

        var bundleUrl = $"/api/catalog/artworks/{artworkId}/bundle";
        var thumbnailUrl = $"/api/catalog/artworks/{artworkId}/thumbnail";
        var artwork = new Artwork
        {
            Id = artworkId,
            Title = title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CategoryId = categoryId,
            CountryCode = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpperInvariant(),
            LicenseType = "Original",
            ThumbnailUrl = thumbnailUrl,
            AssetUrl = bundleUrl,
            BundleChecksum = materialized.Checksum,
            Difficulty = (int)job.Difficulty + 1,
            RegionCount = materialized.RegionCount,
            PublishingStatus = PublishingStatus.Published,
            PublishedAtUtc = DateTime.UtcNow
        };
        artwork.Assets.Add(new ArtworkAsset
        {
            Kind = ArtworkAssetKind.BundleJson,
            Uri = bundleUrl,
            ContentType = "application/json",
            Checksum = materialized.Checksum,
            SizeBytes = new FileInfo(materialized.BundlePath).Length,
            IsPrimary = true
        });
        artwork.Assets.Add(new ArtworkAsset
        {
            Kind = ArtworkAssetKind.Thumbnail,
            Uri = thumbnailUrl,
            ContentType = "image/webp",
            SizeBytes = new FileInfo(materialized.ThumbnailPath).Length,
            IsPrimary = true
        });

        await artworks.AddAsync(artwork, ct);
        job.Status = GenerationJobStatus.Published;
        audit.Add(new AuditLog
        {
            UserId = userId,
            EntityName = nameof(ArtworkGenerationJob),
            EntityId = job.Id.ToString(),
            Action = "GenerationPublished",
            ChangesJson = System.Text.Json.JsonSerializer.Serialize(new { artworkId })
        });

        return artwork;
    }

    private static void EnsureApproved(ArtworkGenerationJob job)
    {
        if (job.Status != GenerationJobStatus.Approved)
            throw new InvalidOperationException("Generation job must be approved before publication.");
        if (string.IsNullOrWhiteSpace(job.ResultManifestPath))
            throw new InvalidOperationException("Generation result is not ready.");
    }

    private static string BuildBatchTitle(string? prefix, string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName)
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Trim();
        stem = string.Join(' ', stem.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(stem))
            stem = "Artwork";

        return string.IsNullOrWhiteSpace(prefix)
            ? stem
            : $"{prefix.Trim()} {stem}";
    }

    private static ArtworkDto Map(Artwork x) => new(
        x.Id, x.Title, x.Description, x.CategoryId, x.CountryCode,
        x.BrandId, x.LicenseAgreementId, x.LicenseType, x.LicenseReference,
        x.ThumbnailUrl, x.AssetUrl, x.BundleChecksum, x.Difficulty,
        x.RegionCount, x.PublishingStatus, x.ScheduledPublishAtUtc,
        x.PublishedAtUtc, x.Assets.Select(a => new ArtworkAssetDto(
            a.Id, a.Kind, a.Uri, a.ContentType, a.Checksum,
            a.SizeBytes, a.IsPrimary)).ToArray());
}
