using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Application.Services;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.UnitTests;

public sealed class GenerationPublishingServiceTests
{
    [Fact]
    public async Task Publish_requires_explicit_approval()
    {
        var job = Job(GenerationJobStatus.PreviewReady, Guid.NewGuid());
        var sut = CreateService(jobs: new FakeJobRepository(job));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.PublishAsync(
                Guid.NewGuid(),
                job.Id,
                new PublishGenerationRequest("Title", Guid.NewGuid(), "EC"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Publish_batch_prevalidates_all_jobs_before_materializing()
    {
        var batchId = Guid.NewGuid();
        var firstAsset = Asset("first.png");
        var secondAsset = Asset("second.png");
        var approved = Job(GenerationJobStatus.Approved, firstAsset.Id, batchId);
        var notApproved = Job(GenerationJobStatus.PreviewReady, secondAsset.Id, batchId);
        using var publication = new FakePublicationStore();
        var artworkRepository = new FakeArtworkRepository();
        var uow = new FakeUnitOfWork();

        var sut = CreateService(
            jobs: new FakeJobRepository(approved, notApproved),
            assets: new FakeSourceAssetRepository(firstAsset, secondAsset),
            publication: publication,
            artworks: artworkRepository,
            categories: new FakeCategoryRepository(ActiveCategory()),
            uow: uow);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.PublishBatchAsync(
                Guid.NewGuid(),
                batchId,
                new PublishGenerationBatchRequest(ActiveCategoryId, "EC", "Pack"),
                CancellationToken.None));

        Assert.Equal(0, publication.MaterializeCalls);
        Assert.Empty(artworkRepository.Items);
        Assert.Equal(0, uow.SaveCalls);
        Assert.Equal(GenerationJobStatus.Approved, approved.Status);
        Assert.Equal(GenerationJobStatus.PreviewReady, notApproved.Status);
    }

    [Fact]
    public async Task Publish_batch_publishes_every_approved_job_with_one_commit()
    {
        var batchId = Guid.NewGuid();
        var firstAsset = Asset("andean_peak.png");
        var secondAsset = Asset("space-cruiser.jpg");
        var first = Job(GenerationJobStatus.Approved, firstAsset.Id, batchId);
        var second = Job(GenerationJobStatus.Approved, secondAsset.Id, batchId);
        using var publication = new FakePublicationStore();
        var artworkRepository = new FakeArtworkRepository();
        var uow = new FakeUnitOfWork();

        var sut = CreateService(
            jobs: new FakeJobRepository(first, second),
            assets: new FakeSourceAssetRepository(firstAsset, secondAsset),
            publication: publication,
            artworks: artworkRepository,
            categories: new FakeCategoryRepository(ActiveCategory()),
            uow: uow);

        var result = await sut.PublishBatchAsync(
            Guid.NewGuid(),
            batchId,
            new PublishGenerationBatchRequest(
                ActiveCategoryId,
                "ec",
                "Colección",
                "Contenido original"),
            CancellationToken.None);

        Assert.Equal(batchId, result.BatchId);
        Assert.Equal(2, result.PublishedCount);
        Assert.Equal(2, result.Artworks.Count);
        Assert.Equal(2, publication.MaterializeCalls);
        Assert.Equal(2, artworkRepository.Items.Count);
        Assert.Equal(1, uow.SaveCalls);
        Assert.All(artworkRepository.Items, x =>
        {
            Assert.Equal(PublishingStatus.Published, x.PublishingStatus);
            Assert.Equal("EC", x.CountryCode);
            Assert.Equal("Contenido original", x.Description);
        });
        Assert.Contains(artworkRepository.Items, x => x.Title == "Colección andean peak");
        Assert.Contains(artworkRepository.Items, x => x.Title == "Colección space cruiser");
        Assert.Equal(GenerationJobStatus.Published, first.Status);
        Assert.Equal(GenerationJobStatus.Published, second.Status);
    }

    [Fact]
    public async Task Publish_batch_assigns_collection_in_order_and_inherits_country()
    {
        var batchId = Guid.NewGuid();
        var firstAsset = Asset("andes-one.png");
        var secondAsset = Asset("andes-two.png");
        var first = Job(GenerationJobStatus.Approved, firstAsset.Id, batchId);
        var second = Job(GenerationJobStatus.Approved, secondAsset.Id, batchId);
        var collection = ActiveCollection();
        collection.Artworks.Add(new CollectionArtwork
        {
            CollectionId = collection.Id,
            ArtworkId = Guid.NewGuid(),
            SortOrder = 7
        });
        using var publication = new FakePublicationStore();
        var artworkRepository = new FakeArtworkRepository();

        var sut = CreateService(
            jobs: new FakeJobRepository(first, second),
            assets: new FakeSourceAssetRepository(firstAsset, secondAsset),
            publication: publication,
            artworks: artworkRepository,
            categories: new FakeCategoryRepository(ActiveCategory()),
            collections: new FakeCollectionRepository(collection));

        var result = await sut.PublishBatchAsync(
            Guid.NewGuid(),
            batchId,
            new PublishGenerationBatchRequest(
                ActiveCategoryId,
                null,
                "Andes",
                null,
                collection.Id),
            CancellationToken.None);

        Assert.Equal(2, result.PublishedCount);
        Assert.Equal(3, collection.Artworks.Count);
        Assert.Equal([7, 8, 9], collection.Artworks.OrderBy(x => x.SortOrder).Select(x => x.SortOrder).ToArray());
        Assert.Equal(
            artworkRepository.Items.Select(x => x.Id).Order(),
            collection.Artworks.Where(x => x.SortOrder >= 8).Select(x => x.ArtworkId).Order());
        Assert.All(artworkRepository.Items, x => Assert.Equal("SA", x.CountryCode));
    }

    [Fact]
    public async Task Publish_batch_rejects_inactive_collection_before_materializing()
    {
        var batchId = Guid.NewGuid();
        var asset = Asset("comic-one.png");
        var job = Job(GenerationJobStatus.Approved, asset.Id, batchId);
        var inactive = ActiveCollection();
        inactive.IsActive = false;
        using var publication = new FakePublicationStore();
        var artworkRepository = new FakeArtworkRepository();

        var sut = CreateService(
            jobs: new FakeJobRepository(job),
            assets: new FakeSourceAssetRepository(asset),
            publication: publication,
            artworks: artworkRepository,
            categories: new FakeCategoryRepository(ActiveCategory()),
            collections: new FakeCollectionRepository(inactive));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.PublishBatchAsync(
                Guid.NewGuid(),
                batchId,
                new PublishGenerationBatchRequest(
                    ActiveCategoryId,
                    "EC",
                    CollectionId: inactive.Id),
                CancellationToken.None));

        Assert.Equal(0, publication.MaterializeCalls);
        Assert.Empty(artworkRepository.Items);
        Assert.Equal(GenerationJobStatus.Approved, job.Status);
    }

    private static readonly Guid ActiveCategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ActiveCollectionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static Category ActiveCategory() => new()
    {
        Id = ActiveCategoryId,
        Name = "Original",
        Slug = "original",
        IsActive = true
    };

    private static Collection ActiveCollection() => new()
    {
        Id = ActiveCollectionId,
        Name = "Andean Originals",
        Slug = "andean-originals",
        CountryCode = "SA",
        IsActive = true
    };

    private static SourceAsset Asset(string fileName) => new()
    {
        Id = Guid.NewGuid(),
        FileName = fileName,
        ContentType = "image/png",
        StoragePath = $"memory://{fileName}",
        SizeBytes = 100,
        Sha256 = new string('a', 64),
        Status = SourceAssetStatus.Validated
    };

    private static ArtworkGenerationJob Job(
        GenerationJobStatus status,
        Guid sourceAssetId,
        Guid? batchId = null) => new()
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            SourceAssetId = sourceAssetId,
            StylePresetId = Guid.NewGuid(),
            Difficulty = GenerationDifficulty.Detailed,
            Status = status,
            ResultManifestPath = "memory://manifest.json",
            CreatedByUserId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow
        };

    private static GenerationPublishingService CreateService(
        FakeJobRepository? jobs = null,
        FakeSourceAssetRepository? assets = null,
        FakePublicationStore? publication = null,
        FakeArtworkRepository? artworks = null,
        FakeCategoryRepository? categories = null,
        FakeCollectionRepository? collections = null,
        FakeUnitOfWork? uow = null) =>
        new(
            jobs ?? new(),
            assets ?? new(),
            new FakeAdjustmentStore(),
            publication ?? new FakePublicationStore(),
            artworks ?? new(),
            categories ?? new FakeCategoryRepository(ActiveCategory()),
            collections ?? new FakeCollectionRepository(),
            new FakeAuditRepository(),
            uow ?? new());

    private sealed class FakeJobRepository(params ArtworkGenerationJob[] seed) : IGenerationJobRepository
    {
        public List<ArtworkGenerationJob> Items { get; } = [.. seed];
        public Task<ArtworkGenerationJob?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyCollection<GenerationJobDto>> ListAsync(int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<GenerationJobDto>>([]);
        public Task<IReadOnlyCollection<ArtworkGenerationJob>> ListByBatchAsync(Guid batchId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<ArtworkGenerationJob>>(Items.Where(x => x.BatchId == batchId).ToArray());
        public Task AddAsync(ArtworkGenerationJob job, CancellationToken ct)
        {
            Items.Add(job);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSourceAssetRepository(params SourceAsset[] seed) : ISourceAssetRepository
    {
        private readonly List<SourceAsset> items = [.. seed];
        public Task<SourceAsset?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(items.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyCollection<SourceAssetDto>> ListAsync(int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<SourceAssetDto>>([]);
        public Task AddAsync(SourceAsset asset, CancellationToken ct)
        {
            items.Add(asset);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAdjustmentStore : IGenerationAdjustmentStore
    {
        public Task<IReadOnlyCollection<RegionAdjustmentDto>> ListAsync(string resultManifestPath, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<RegionAdjustmentDto>>([]);
        public Task<RegionAdjustmentDto?> UpsertAsync(string resultManifestPath, int regionId, Guid userId, RegionAdjustmentRequest request, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<bool> DeleteAsync(string resultManifestPath, int regionId, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class FakePublicationStore : IGenerationPublicationStore, IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), $"appcoloreando-publish-unit-{Guid.NewGuid():N}");
        public int MaterializeCalls { get; private set; }

        public FakePublicationStore() => Directory.CreateDirectory(root);

        public async Task<GenerationPublicationAssets?> MaterializeAsync(
            Guid artworkId,
            string resultManifestPath,
            IReadOnlyCollection<RegionAdjustmentDto> adjustments,
            CancellationToken ct)
        {
            MaterializeCalls++;
            var folder = Path.Combine(root, artworkId.ToString("N"));
            Directory.CreateDirectory(folder);
            var bundle = Path.Combine(folder, "bundle.json");
            var thumbnail = Path.Combine(folder, "thumbnail.webp");
            await File.WriteAllTextAsync(bundle, "{}", ct);
            await File.WriteAllBytesAsync(thumbnail, [1, 2, 3, 4], ct);
            return new GenerationPublicationAssets(bundle, thumbnail, 240, $"checksum-{artworkId:N}");
        }

        public Task<GenerationArtifactDto?> ReadPublishedAsync(Guid artworkId, string artifactKind, CancellationToken ct) =>
            Task.FromResult<GenerationArtifactDto?>(null);

        public void Dispose()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FakeArtworkRepository : IArtworkRepository
    {
        public List<Artwork> Items { get; } = [];
        public Task<bool> PublishedExistsAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Items.Any(x => x.Id == id && x.PublishingStatus == PublishingStatus.Published));
        public Task<Artwork?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<PageResult<ArtworkDto>> SearchAsync(CatalogQuery query, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task AddAsync(Artwork artwork, CancellationToken ct)
        {
            Items.Add(artwork);
            return Task.CompletedTask;
        }
        public Task<int> CountPublishedAsync(CancellationToken ct) =>
            Task.FromResult(Items.Count(x => x.PublishingStatus == PublishingStatus.Published));
    }

    private sealed class FakeCategoryRepository(params Category[] seed) : ICategoryRepository
    {
        private readonly List<Category> items = [.. seed];
        public Task<Category?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(items.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyCollection<CategoryDto>> ListAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<CategoryDto>>([]);
        public Task AddAsync(Category category, CancellationToken ct)
        {
            items.Add(category);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCollectionRepository(params Collection[] seed) : ICollectionRepository
    {
        private readonly List<Collection> items = [.. seed];
        public Task<IReadOnlyCollection<CollectionDto>> ListAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<CollectionDto>>([]);
        public Task<Collection?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(items.SingleOrDefault(x => x.Id == id));
        public void Add(Collection collection) => items.Add(collection);
    }

    private sealed class FakeAuditRepository : IAuditRepository
    {
        public void Add(AuditLog audit) { }
        public Task<IReadOnlyCollection<AuditLogDto>> ListAsync(int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<AuditLogDto>>([]);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}
