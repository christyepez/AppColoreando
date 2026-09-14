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

    public async Task<GenerationBatchDto> CreateGenerationBatchAsync(
        Guid userId, CreateGenerationBatchRequest request, CancellationToken ct)
    {
        var sourceIds = request.SourceAssetIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray() ?? [];
        if (sourceIds.Length == 0)
            throw new ArgumentException("At least one source asset is required.");
        if (sourceIds.Length > 100)
            throw new ArgumentException("A generation batch supports at most 100 source assets.");

        var batchId = Guid.NewGuid();
        var created = new List<GenerationJobDto>(sourceIds.Length);
        foreach (var sourceAssetId in sourceIds)
        {
            ct.ThrowIfCancellationRequested();
            created.Add(await CreateGenerationJobAsync(userId,
                new CreateGenerationJobRequest(sourceAssetId, request.StylePresetId, request.Difficulty), ct));
        }
        audit.Add(new AuditLog
        {
            UserId = userId,
            EntityName = "GenerationBatch",
            EntityId = batchId.ToString(),
            Action = "GenerationBatchQueued",
            ChangesJson = $"{{\"requestedCount\":{sourceIds.Length},\"queuedCount\":{created.Count}}}"
        });
        await uow.SaveChangesAsync(ct);
        return new GenerationBatchDto(batchId, sourceIds.Length, created.Count, created);
    }
    public async Task<GenerationBatchStatusDto> GetGenerationBatchAsync(Guid batchId, CancellationToken ct)
    {
        var items = await jobs.ListByBatchAsync(batchId, ct);
        if (items.Count == 0) throw new KeyNotFoundException("Generation batch not found.");
        return MapBatch(batchId, items);
    }

    public async Task<GenerationBatchStatusDto> RetryGenerationBatchAsync(Guid userId, Guid batchId, CancellationToken ct)
    {
        var items = await jobs.ListByBatchAsync(batchId, ct);
        if (items.Count == 0) throw new KeyNotFoundException("Generation batch not found.");
        foreach (var job in items.Where(x => x.Status == GenerationJobStatus.Failed))
        {
            job.Status = GenerationJobStatus.Queued;
            job.ErrorCode = null; job.ErrorMessage = null; job.StartedAtUtc = null; job.CompletedAtUtc = null;
            await queue.EnqueueAsync(job.Id, ct);
        }
        audit.Add(new AuditLog { UserId = userId, EntityName = "GenerationBatch", EntityId = batchId.ToString(), Action = "GenerationBatchRetried" });
        await uow.SaveChangesAsync(ct);
        return MapBatch(batchId, items);
    }

    public async Task<GenerationBatchStatusDto> CancelGenerationBatchAsync(Guid userId, Guid batchId, CancellationToken ct)
    {
        var items = await jobs.ListByBatchAsync(batchId, ct);
        if (items.Count == 0) throw new KeyNotFoundException("Generation batch not found.");
        foreach (var job in items.Where(x => x.Status is GenerationJobStatus.Pending or GenerationJobStatus.Queued))
        {
            job.Status = GenerationJobStatus.Cancelled;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.ErrorCode = "CANCELLED";
            job.ErrorMessage = "Cancelled before processing started.";
        }
        audit.Add(new AuditLog { UserId = userId, EntityName = "GenerationBatch", EntityId = batchId.ToString(), Action = "GenerationBatchCancelled" });
        await uow.SaveChangesAsync(ct);
        return MapBatch(batchId, items);
    }

    private static GenerationBatchStatusDto MapBatch(Guid batchId, IReadOnlyCollection<ArtworkGenerationJob> items)
    {
        var total = items.Count;
        var queued = items.Count(x => x.Status is GenerationJobStatus.Pending or GenerationJobStatus.Queued);
        var running = items.Count(x => x.Status == GenerationJobStatus.Running);
        var ready = items.Count(x => x.Status is GenerationJobStatus.PreviewReady or GenerationJobStatus.NeedsReview or GenerationJobStatus.Approved or GenerationJobStatus.Published);
        var failed = items.Count(x => x.Status == GenerationJobStatus.Failed);
        var cancelled = items.Count(x => x.Status == GenerationJobStatus.Cancelled);
        var completed = ready + failed + cancelled;
        var progress = total == 0 ? 0 : Math.Round(completed * 100.0 / total, 2);
        return new GenerationBatchStatusDto(batchId, total, queued, running, ready, failed, cancelled, completed, progress, items.Select(Map).ToArray());
    }
    public async Task<GenerationJobDto> SubmitForReviewAsync(Guid userId, Guid id, EditorialTransitionRequest request, CancellationToken ct)
    {
        var job = await jobs.GetAsync(id, ct) ?? throw new KeyNotFoundException("Generation job not found.");
        if (job.Status != GenerationJobStatus.PreviewReady) throw new InvalidOperationException("Only preview-ready jobs can be submitted for review.");
        return await TransitionAsync(userId, job, GenerationJobStatus.NeedsReview, "GenerationSubmittedForReview", request.Note, ct);
    }

    public async Task<GenerationJobDto> ApproveGenerationAsync(Guid userId, Guid id, EditorialTransitionRequest request, CancellationToken ct)
    {
        var job = await jobs.GetAsync(id, ct) ?? throw new KeyNotFoundException("Generation job not found.");
        if (job.Status != GenerationJobStatus.NeedsReview) throw new InvalidOperationException("Only jobs in review can be approved.");
        return await TransitionAsync(userId, job, GenerationJobStatus.Approved, "GenerationApproved", request.Note, ct);
    }

    public async Task<GenerationJobDto> ReturnToPreviewAsync(Guid userId, Guid id, EditorialTransitionRequest request, CancellationToken ct)
    {
        var job = await jobs.GetAsync(id, ct) ?? throw new KeyNotFoundException("Generation job not found.");
        if (job.Status is not (GenerationJobStatus.NeedsReview or GenerationJobStatus.Approved)) throw new InvalidOperationException("Only reviewed or approved jobs can return to preview.");
        return await TransitionAsync(userId, job, GenerationJobStatus.PreviewReady, "GenerationReturnedToPreview", request.Note, ct);
    }

    private async Task<GenerationJobDto> TransitionAsync(Guid userId, ArtworkGenerationJob job, GenerationJobStatus status, string action, string? note, CancellationToken ct)
    {
        job.Status = status;
        audit.Add(new AuditLog { UserId = userId, EntityName = nameof(ArtworkGenerationJob), EntityId = job.Id.ToString(), Action = action, ChangesJson = System.Text.Json.JsonSerializer.Serialize(new { note }) });
        await uow.SaveChangesAsync(ct);
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
