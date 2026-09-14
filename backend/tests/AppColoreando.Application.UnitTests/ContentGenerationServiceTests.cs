using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Application.Services;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.UnitTests;

public sealed class ContentGenerationServiceTests
{
    [Fact]
    public async Task Upload_rejects_unsupported_content_type()
    {
        var sut = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() => sut.UploadSourceAssetAsync(
            Guid.NewGuid(), "bad.txt", "text/plain", 3,
            new MemoryStream([1, 2, 3]), CancellationToken.None));
    }

    [Fact]
    public async Task Upload_persists_hash_and_storage_path()
    {
        var assets = new FakeAssetRepository();
        var storage = new FakeStorage();
        var sut = CreateService(assets: assets, storage: storage);
        var dto = await sut.UploadSourceAssetAsync(Guid.NewGuid(), "duck.png", "image/png", 4,
            new MemoryStream([1, 2, 3, 4]), CancellationToken.None);

        Assert.Equal(SourceAssetStatus.Validated, dto.Status);
        Assert.Equal("memory://duck.png", dto.StoragePath);
        Assert.Equal(64, dto.Sha256.Length);
        Assert.Single(assets.Items);
    }

    [Fact]
    public async Task Create_job_persists_then_enqueues()
    {
        var asset = new SourceAsset { Id = Guid.NewGuid(), Status = SourceAssetStatus.Validated };
        var preset = new StylePreset { Id = Guid.NewGuid(), Code = "natural", IsActive = true };
        var assets = new FakeAssetRepository(asset);
        var presets = new FakePresetRepository(preset);
        var jobs = new FakeJobRepository();
        var queue = new FakeQueue();
        var sut = CreateService(assets, presets, jobs, queue: queue);

        var result = await sut.CreateGenerationJobAsync(Guid.NewGuid(),
            new CreateGenerationJobRequest(asset.Id, preset.Id, GenerationDifficulty.Normal),
            CancellationToken.None);

        Assert.Equal(GenerationJobStatus.Queued, result.Status);
        Assert.Single(jobs.Items);
        Assert.Equal(result.Id, queue.LastJobId);
    }

    [Fact]
    public async Task Create_batch_deduplicates_assets_and_queues_each_job()
    {
        var first = new SourceAsset { Id = Guid.NewGuid(), Status = SourceAssetStatus.Validated };
        var second = new SourceAsset { Id = Guid.NewGuid(), Status = SourceAssetStatus.Validated };
        var preset = new StylePreset { Id = Guid.NewGuid(), Code = "natural", IsActive = true };
        var jobs = new FakeJobRepository();
        var sut = CreateService(new FakeAssetRepository(first, second), new FakePresetRepository(preset), jobs);

        var result = await sut.CreateGenerationBatchAsync(Guid.NewGuid(),
            new CreateGenerationBatchRequest([first.Id, second.Id, first.Id], preset.Id),
            CancellationToken.None);

        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(2, result.QueuedCount);
        Assert.Equal(2, result.Jobs.Count);
        Assert.Equal(2, jobs.Items.Count);
    }

    [Fact]
    public async Task Create_batch_rejects_more_than_one_hundred_assets()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray();
        var sut = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateGenerationBatchAsync(
            Guid.NewGuid(), new CreateGenerationBatchRequest(ids, Guid.NewGuid()), CancellationToken.None));
    }
    [Fact]
    public async Task Batch_status_reports_progress_and_cancelled_items()
    {
        var batchId = Guid.NewGuid();
        var jobs = new FakeJobRepository();
        jobs.Items.AddRange([
            new ArtworkGenerationJob { BatchId = batchId, Status = GenerationJobStatus.PreviewReady },
            new ArtworkGenerationJob { BatchId = batchId, Status = GenerationJobStatus.Failed },
            new ArtworkGenerationJob { BatchId = batchId, Status = GenerationJobStatus.Queued }
        ]);
        var sut = CreateService(jobs: jobs);
        var status = await sut.GetGenerationBatchAsync(batchId, CancellationToken.None);
        Assert.Equal(3, status.TotalCount);
        Assert.Equal(2, status.CompletedCount);
        Assert.Equal(66.67, status.ProgressPercent);
    }

    [Fact]
    public async Task Cancel_batch_only_cancels_not_started_jobs()
    {
        var batchId = Guid.NewGuid();
        var jobs = new FakeJobRepository();
        jobs.Items.AddRange([
            new ArtworkGenerationJob { BatchId = batchId, Status = GenerationJobStatus.Queued },
            new ArtworkGenerationJob { BatchId = batchId, Status = GenerationJobStatus.Running }
        ]);
        var sut = CreateService(jobs: jobs);
        var status = await sut.CancelGenerationBatchAsync(Guid.NewGuid(), batchId, CancellationToken.None);
        Assert.Equal(1, status.CancelledCount);
        Assert.Equal(1, status.RunningCount);
    }
    [Fact]
    public async Task Editorial_flow_requires_review_before_approval()
    {
        var jobs = new FakeJobRepository();
        var job = new ArtworkGenerationJob { Id = Guid.NewGuid(), Status = GenerationJobStatus.PreviewReady };
        jobs.Items.Add(job);
        var sut = CreateService(jobs: jobs);
        var reviewed = await sut.SubmitForReviewAsync(Guid.NewGuid(), job.Id, new EditorialTransitionRequest("ready"), CancellationToken.None);
        Assert.Equal(GenerationJobStatus.NeedsReview, reviewed.Status);
        var approved = await sut.ApproveGenerationAsync(Guid.NewGuid(), job.Id, new EditorialTransitionRequest("ok"), CancellationToken.None);
        Assert.Equal(GenerationJobStatus.Approved, approved.Status);
    }

    [Fact]
    public async Task Editorial_flow_rejects_approval_without_review()
    {
        var jobs = new FakeJobRepository();
        var job = new ArtworkGenerationJob { Id = Guid.NewGuid(), Status = GenerationJobStatus.PreviewReady };
        jobs.Items.Add(job);
        var sut = CreateService(jobs: jobs);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApproveGenerationAsync(Guid.NewGuid(), job.Id, new EditorialTransitionRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Editorial_flow_can_return_approved_job_to_preview()
    {
        var jobs = new FakeJobRepository();
        var job = new ArtworkGenerationJob { Id = Guid.NewGuid(), Status = GenerationJobStatus.Approved };
        jobs.Items.Add(job);
        var sut = CreateService(jobs: jobs);
        var result = await sut.ReturnToPreviewAsync(Guid.NewGuid(), job.Id, new EditorialTransitionRequest("changes requested"), CancellationToken.None);
        Assert.Equal(GenerationJobStatus.PreviewReady, result.Status);
    }
    private static ContentGenerationService CreateService(
        FakeAssetRepository? assets = null,
        FakePresetRepository? presets = null,
        FakeJobRepository? jobs = null,
        FakeStorage? storage = null,
        FakeArtifactReader? artifacts = null,
        FakeAdjustmentStore? adjustments = null,
        FakeQueue? queue = null) =>
        new(assets ?? new(), presets ?? new(), jobs ?? new(), storage ?? new(),
            artifacts ?? new(), adjustments ?? new(), queue ?? new(), new FakeAuditRepository(), new FakeUnitOfWork());

    private sealed class FakeAssetRepository(params SourceAsset[] seed) : ISourceAssetRepository
    {
        public List<SourceAsset> Items { get; } = [.. seed];
        public Task<SourceAsset?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyCollection<SourceAssetDto>> ListAsync(int take, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SourceAssetDto>>([]);
        public Task AddAsync(SourceAsset asset, CancellationToken ct) { Items.Add(asset); return Task.CompletedTask; }
    }

    private sealed class FakePresetRepository(params StylePreset[] seed) : IStylePresetRepository
    {
        private readonly List<StylePreset> items = [.. seed];
        public Task<StylePreset?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(items.SingleOrDefault(x => x.Id == id));
        public Task<StylePreset?> GetByCodeAsync(string code, CancellationToken ct) => Task.FromResult(items.SingleOrDefault(x => x.Code == code));
        public Task<IReadOnlyCollection<StylePresetDto>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<StylePresetDto>>([]);
        public void Add(StylePreset preset) => items.Add(preset);
    }

    private sealed class FakeJobRepository : IGenerationJobRepository
    {
        public List<ArtworkGenerationJob> Items { get; } = [];
        public Task<ArtworkGenerationJob?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyCollection<GenerationJobDto>> ListAsync(int take, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<GenerationJobDto>>([]);
        public Task<IReadOnlyCollection<ArtworkGenerationJob>> ListByBatchAsync(Guid batchId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ArtworkGenerationJob>>(Items.Where(x => x.BatchId == batchId).ToArray());
        public Task AddAsync(ArtworkGenerationJob job, CancellationToken ct) { Items.Add(job); return Task.CompletedTask; }
    }

    private sealed class FakeStorage : ISourceAssetStorage
    {
        public Task<string> SaveAsync(Guid assetId, string fileName, Stream content, CancellationToken ct) =>
            Task.FromResult($"memory://{fileName}");
    }

    private sealed class FakeArtifactReader : IGenerationArtifactReader
    {
        public Task<GenerationArtifactDto?> ReadAsync(string resultManifestPath, string artifactKind, CancellationToken ct) =>
            Task.FromResult<GenerationArtifactDto?>(new([1, 2, 3], "image/webp", "preview.webp"));
    }

    private sealed class FakeAdjustmentStore : IGenerationAdjustmentStore
    {
        public Task<IReadOnlyCollection<RegionAdjustmentDto>> ListAsync(string path, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<RegionAdjustmentDto>>([]);
        public Task<RegionAdjustmentDto?> UpsertAsync(string path, int regionId, Guid userId, RegionAdjustmentRequest request, CancellationToken ct) => Task.FromResult<RegionAdjustmentDto?>(new(regionId, request.ColorHex, request.SemanticTag, request.SemanticRole, request.LabelVisibleAtBase, request.Note, userId, DateTime.UtcNow));
        public Task<bool> DeleteAsync(string path, int regionId, CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeQueue : IGenerationJobQueue
    {
        public Guid? LastJobId { get; private set; }
        public Task EnqueueAsync(Guid jobId, CancellationToken ct) { LastJobId = jobId; return Task.CompletedTask; }
    }

    private sealed class FakeAuditRepository : IAuditRepository
    {
        public void Add(AuditLog audit) { }
        public Task<IReadOnlyCollection<AuditLogDto>> ListAsync(int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<AuditLogDto>>([]);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
    }
}
