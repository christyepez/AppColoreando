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
