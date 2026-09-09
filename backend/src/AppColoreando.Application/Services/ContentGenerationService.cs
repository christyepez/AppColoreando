using System.Security.Cryptography;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Services;

public sealed class ContentGenerationService(
    ISourceAssetRepository assets,
    IStylePresetRepository presets,
    IGenerationJobRepository jobs,
    ISourceAssetStorage storage,
    IGenerationArtifactReader artifacts,
    IGenerationAdjustmentStore adjustments,
    IGenerationJobQueue queue,
    IAuditRepository audit,
    IUnitOfWork uow) : IContentGenerationService
{
    private static readonly HashSet<string> AllowedTypes =
        ["image/jpeg", "image/png", "image/svg+xml"];
    private const long MaxAssetBytes = 25 * 1024 * 1024;

    public async Task<SourceAssetDto> UploadSourceAssetAsync(
        Guid userId, string fileName, string contentType,
        long sizeBytes, Stream content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.");
        if (!AllowedTypes.Contains(contentType.ToLowerInvariant()))
            throw new ArgumentException("Only JPEG, PNG and SVG source assets are supported.");
        if (sizeBytes <= 0 || sizeBytes > MaxAssetBytes)
            throw new ArgumentException($"Source asset must be between 1 byte and {MaxAssetBytes} bytes.");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var entity = new SourceAsset
        {
            FileName = Path.GetFileName(fileName),
            ContentType = contentType.ToLowerInvariant(),
            SizeBytes = bytes.LongLength,
            Sha256 = hash,
            Status = SourceAssetStatus.Validated,
            CreatedByUserId = userId
        };

        await using var upload = new MemoryStream(bytes, writable: false);
        entity.StoragePath = await storage.SaveAsync(entity.Id, entity.FileName, upload, ct);
        await assets.AddAsync(entity, ct);
        audit.Add(new AuditLog
        {
            UserId = userId,
            EntityName = nameof(SourceAsset),
            EntityId = entity.Id.ToString(),
            Action = "SourceAssetUploaded",
            ChangesJson = $"{{\"sha256\":\"{hash}\",\"sizeBytes\":{bytes.LongLength}}}"
        });
        await uow.SaveChangesAsync(ct);
        return Map(entity);
    }

    public Task<IReadOnlyCollection<SourceAssetDto>> GetSourceAssetsAsync(int take, CancellationToken ct) =>
        assets.ListAsync(Math.Clamp(take, 1, 200), ct);

    public Task<IReadOnlyCollection<StylePresetDto>> GetStylePresetsAsync(CancellationToken ct) =>
        presets.ListAsync(ct);

    public async Task<GenerationJobDto> CreateGenerationJobAsync(
        Guid userId, CreateGenerationJobRequest request, CancellationToken ct)
    {
        var asset = await assets.GetAsync(request.SourceAssetId, ct)
            ?? throw new KeyNotFoundException("Source asset not found.");
        if (asset.Status is SourceAssetStatus.Rejected)
            throw new InvalidOperationException("Rejected source assets cannot be processed.");

        var preset = await presets.GetAsync(request.StylePresetId, ct)
            ?? throw new KeyNotFoundException("Style preset not found.");
        if (!preset.IsActive)
            throw new InvalidOperationException("Style preset is inactive.");

        var job = new ArtworkGenerationJob
        {
            SourceAssetId = asset.Id,
            StylePresetId = preset.Id,
            Difficulty = request.Difficulty,
            Status = GenerationJobStatus.Queued,
            CreatedByUserId = userId
        };
        await jobs.AddAsync(job, ct);
        audit.Add(new AuditLog
        {
            UserId = userId,
            EntityName = nameof(ArtworkGenerationJob),
            EntityId = job.Id.ToString(),
            Action = "GenerationJobQueued",
            ChangesJson = $"{{\"preset\":\"{preset.Code}\",\"difficulty\":\"{request.Difficulty}\"}}"
        });
        await uow.SaveChangesAsync(ct);
        await queue.EnqueueAsync(job.Id, ct);
        return Map(job);
    }

    public async Task<GenerationJobDto?> GetGenerationJobAsync(Guid id, CancellationToken ct)
    {
        var entity = await jobs.GetAsync(id, ct);
        return entity is null ? null : Map(entity);
    }

    public Task<IReadOnlyCollection<GenerationJobDto>> GetGenerationJobsAsync(int take, CancellationToken ct) =>
        jobs.ListAsync(Math.Clamp(take, 1, 200), ct);

    public async Task<IReadOnlyCollection<RegionAdjustmentDto>> GetGenerationAdjustmentsAsync(Guid jobId, CancellationToken ct)
    {
        var job = await jobs.GetAsync(jobId, ct) ?? throw new KeyNotFoundException("Generation job not found.");
        if (string.IsNullOrWhiteSpace(job.ResultManifestPath)) return [];
        return await adjustments.ListAsync(job.ResultManifestPath, ct);
    }

    public async Task<RegionAdjustmentDto> UpsertGenerationAdjustmentAsync(Guid userId, Guid jobId, int regionId, RegionAdjustmentRequest request, CancellationToken ct)
    {
        var job = await jobs.GetAsync(jobId, ct) ?? throw new KeyNotFoundException("Generation job not found.");
        if (string.IsNullOrWhiteSpace(job.ResultManifestPath)) throw new InvalidOperationException("Generation result is not ready.");
        var result = await adjustments.UpsertAsync(job.ResultManifestPath, regionId, userId, request, ct)
            ?? throw new KeyNotFoundException("Generated region not found.");
        audit.Add(new AuditLog { UserId = userId, EntityName = nameof(ArtworkGenerationJob), EntityId = job.Id.ToString(), Action = "GenerationRegionAdjusted", ChangesJson = $"{{\"regionId\":{regionId}}}" });
        await uow.SaveChangesAsync(ct);
        return result;
    }

    public async Task DeleteGenerationAdjustmentAsync(Guid userId, Guid jobId, int regionId, CancellationToken ct)
    {
        var job = await jobs.GetAsync(jobId, ct) ?? throw new KeyNotFoundException("Generation job not found.");
        if (string.IsNullOrWhiteSpace(job.ResultManifestPath)) return;
        if (!await adjustments.DeleteAsync(job.ResultManifestPath, regionId, ct)) return;
        audit.Add(new AuditLog { UserId = userId, EntityName = nameof(ArtworkGenerationJob), EntityId = job.Id.ToString(), Action = "GenerationRegionAdjustmentDeleted", ChangesJson = $"{{\"regionId\":{regionId}}}" });
        await uow.SaveChangesAsync(ct);
    }

    public async Task<GenerationArtifactDto?> GetGenerationArtifactAsync(Guid jobId, string artifactKind, CancellationToken ct)
    {
        var job = await jobs.GetAsync(jobId, ct);
        if (job is null || string.IsNullOrWhiteSpace(job.ResultManifestPath)) return null;
        return await artifacts.ReadAsync(job.ResultManifestPath, artifactKind, ct);
    }

    private static SourceAssetDto Map(SourceAsset x) =>
        new(x.Id, x.FileName, x.ContentType, x.StoragePath, x.SizeBytes,
            x.Width, x.Height, x.Sha256, x.Status, x.CreatedAtUtc);

    private static GenerationJobDto Map(ArtworkGenerationJob x) =>
        new(x.Id, x.SourceAssetId, x.StylePresetId, x.Difficulty, x.Status,
            x.ErrorCode, x.ErrorMessage, x.ProcessorJobId, x.ResultManifestPath,
            x.CreatedAtUtc, x.StartedAtUtc, x.CompletedAtUtc);
}
