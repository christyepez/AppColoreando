using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Services;

public sealed class GenerationPublishingService(
    IGenerationJobRepository jobs,
    IGenerationAdjustmentStore adjustments,
    IGenerationPublicationStore publication,
    IArtworkRepository artworks,
    ICategoryRepository categories,
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
        if (string.IsNullOrWhiteSpace(job.ResultManifestPath))
            throw new InvalidOperationException("Generation result is not ready.");
        var category = await categories.GetAsync(request.CategoryId, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        if (!category.IsActive)
            throw new InvalidOperationException("Category is inactive.");

        if (job.Status is GenerationJobStatus.Failed or GenerationJobStatus.Pending or GenerationJobStatus.Queued or GenerationJobStatus.Running)
            throw new InvalidOperationException("Generation job is not publishable yet.");

        var artworkId = Guid.NewGuid();
        var overlay = await adjustments.ListAsync(job.ResultManifestPath, ct);
        var published = await publication.MaterializeAsync(
            artworkId, job.ResultManifestPath, overlay, ct)
            ?? throw new InvalidOperationException("Generation bundle could not be materialized.");
        var bundleUrl = $"/api/catalog/artworks/{artworkId}/bundle";
        var thumbnailUrl = $"/api/catalog/artworks/{artworkId}/thumbnail";
        var artwork = new Artwork
        {
            Id = artworkId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CategoryId = request.CategoryId,
            CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim().ToUpperInvariant(),
            LicenseType = "Original",
            ThumbnailUrl = thumbnailUrl,
            AssetUrl = bundleUrl,
            BundleChecksum = published.Checksum,
            Difficulty = (int)job.Difficulty + 1,
            RegionCount = published.RegionCount,
            PublishingStatus = PublishingStatus.Published,
            PublishedAtUtc = DateTime.UtcNow
        };
        artwork.Assets.Add(new ArtworkAsset
        {
            Kind = ArtworkAssetKind.BundleJson,
            Uri = bundleUrl,
            ContentType = "application/json",
            Checksum = published.Checksum,
            SizeBytes = new FileInfo(published.BundlePath).Length,
            IsPrimary = true
        });
        artwork.Assets.Add(new ArtworkAsset
        {
            Kind = ArtworkAssetKind.Thumbnail,
            Uri = thumbnailUrl,
            ContentType = "image/webp",
            SizeBytes = new FileInfo(published.ThumbnailPath).Length,
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
        await uow.SaveChangesAsync(ct);

        return Map(artwork);
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
