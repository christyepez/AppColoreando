using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Application.Services;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.UnitTests;

public sealed class GenerationPublishingServiceTests
{
    [Theory]
    [InlineData(GenerationJobStatus.PreviewReady)]
    [InlineData(GenerationJobStatus.NeedsReview)]
    [InlineData(GenerationJobStatus.Published)]
    public async Task Publish_requires_explicit_approved_status(GenerationJobStatus status)
    {
        var job = new ArtworkGenerationJob
        {
            Id = Guid.NewGuid(),
            Status = status,
            ResultManifestPath = "/tmp/generated/manifest.json"
        };
        var sut = new GenerationPublishingService(
            new FakeJobRepository(job),
            new FakeAdjustmentStore(),
            new FakePublicationStore(),
            new FakeArtworkRepository(),
            new FakeCategoryRepository(),
            new FakeAuditRepository(),
            new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.PublishAsync(
                Guid.NewGuid(),
                job.Id,
                new PublishGenerationRequest(
                    "Smoke artwork",
                    Guid.NewGuid(),
                    "EC",
                    "approval gate regression"),
                CancellationToken.None));

        Assert.Contains("approved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeJobRepository(ArtworkGenerationJob job) : IGenerationJobRepository
    {
        public Task<ArtworkGenerationJob?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<ArtworkGenerationJob?>(id == job.Id ? job : null);

        public Task<IReadOnlyCollection<GenerationJobDto>> ListAsync(int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<GenerationJobDto>>([]);

        public Task<IReadOnlyCollection<ArtworkGenerationJob>> ListByBatchAsync(Guid batchId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<ArtworkGenerationJob>>([]);

        public Task AddAsync(ArtworkGenerationJob item, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeAdjustmentStore : IGenerationAdjustmentStore
    {
        public Task<IReadOnlyCollection<RegionAdjustmentDto>> ListAsync(string resultManifestPath, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<RegionAdjustmentDto>>([]);

        public Task<RegionAdjustmentDto?> UpsertAsync(
            string resultManifestPath,
            int regionId,
            Guid userId,
            RegionAdjustmentRequest request,
            CancellationToken ct) =>
            Task.FromResult<RegionAdjustmentDto?>(null);

        public Task<bool> DeleteAsync(string resultManifestPath, int regionId, CancellationToken ct) =>
            Task.FromResult(false);
    }

    private sealed class FakePublicationStore : IGenerationPublicationStore
    {
        public Task<GenerationPublicationAssets?> MaterializeAsync(
            Guid artworkId,
            string resultManifestPath,
            IReadOnlyCollection<RegionAdjustmentDto> adjustments,
            CancellationToken ct) =>
            throw new InvalidOperationException("Publication store must not be reached before approval.");

        public Task<GenerationArtifactDto?> ReadPublishedAsync(
            Guid artworkId,
            string artifactKind,
            CancellationToken ct) =>
            Task.FromResult<GenerationArtifactDto?>(null);
    }

    private sealed class FakeArtworkRepository : IArtworkRepository
    {
        public Task<bool> PublishedExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task<Artwork?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<Artwork?>(null);
        public Task<PageResult<ArtworkDto>> SearchAsync(CatalogQuery query, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task AddAsync(Artwork artwork, CancellationToken ct) => Task.CompletedTask;
        public Task<int> CountPublishedAsync(CancellationToken ct) => Task.FromResult(0);
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public Task<Category?> GetAsync(Guid id, CancellationToken ct) =>
            throw new InvalidOperationException("Category lookup must not be reached before approval.");

        public Task<IReadOnlyCollection<CategoryDto>> ListAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<CategoryDto>>([]);

        public Task AddAsync(Category category, CancellationToken ct) => Task.CompletedTask;
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


    [Fact]
    public async Task Publish_preserves_source_license_metadata()
    {
        var job = new ArtworkGenerationJob
        {
            Id = Guid.NewGuid(),
            Status = GenerationJobStatus.Approved,
            ResultManifestPath = "/tmp/generated/manifest.json"
        };
        var categoryId = Guid.NewGuid();
        var artworkRepository = new CapturingArtworkRepository();
        var sut = new GenerationPublishingService(
            new FakeJobRepository(job),
            new FakeAdjustmentStore(),
            new SuccessfulPublicationStore(),
            artworkRepository,
            new ExistingCategoryRepository(categoryId),
            new FakeAuditRepository(),
            new FakeUnitOfWork());

        await sut.PublishAsync(
            Guid.NewGuid(),
            job.Id,
            new PublishGenerationRequest(
                "Licensed artwork",
                categoryId,
                "EC",
                "source",
                "CC0",
                "https://commons.wikimedia.org/wiki/File:Example.jpg"),
            CancellationToken.None);

        Assert.NotNull(artworkRepository.Added);
        Assert.Equal("CC0", artworkRepository.Added!.LicenseType);
        Assert.Equal("https://commons.wikimedia.org/wiki/File:Example.jpg", artworkRepository.Added.LicenseReference);
    }

    private sealed class SuccessfulPublicationStore : IGenerationPublicationStore
    {
        public Task<GenerationPublicationAssets?> MaterializeAsync(
            Guid artworkId,
            string resultManifestPath,
            IReadOnlyCollection<RegionAdjustmentDto> adjustments,
            CancellationToken ct)
        {
            var dir = Path.Combine(Path.GetTempPath(), "appcoloreando-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var bundle = Path.Combine(dir, "bundle.json");
            var thumb = Path.Combine(dir, "thumbnail.webp");
            File.WriteAllText(bundle, "{}");
            File.WriteAllBytes(thumb, [1, 2, 3]);
            return Task.FromResult<GenerationPublicationAssets?>(
                new GenerationPublicationAssets(bundle, thumb, 42, "abc"));
        }

        public Task<GenerationArtifactDto?> ReadPublishedAsync(Guid artworkId, string artifactKind, CancellationToken ct) =>
            Task.FromResult<GenerationArtifactDto?>(null);
    }

    private sealed class CapturingArtworkRepository : IArtworkRepository
    {
        public Artwork? Added { get; private set; }
        public Task<bool> PublishedExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task<Artwork?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<Artwork?>(null);
        public Task<PageResult<ArtworkDto>> SearchAsync(CatalogQuery query, CancellationToken ct) => throw new NotSupportedException();
        public Task AddAsync(Artwork artwork, CancellationToken ct) { Added = artwork; return Task.CompletedTask; }
        public Task<int> CountPublishedAsync(CancellationToken ct) => Task.FromResult(0);
    }

    private sealed class ExistingCategoryRepository(Guid categoryId) : ICategoryRepository
    {
        public Task<Category?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Category?>(id == categoryId ? new Category { Id = categoryId, Name = "Test", Slug = "test", IsActive = true } : null);
        public Task<IReadOnlyCollection<CategoryDto>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<CategoryDto>>([]);
        public Task AddAsync(Category category, CancellationToken ct) => Task.CompletedTask;
    }
