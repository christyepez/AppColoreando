using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Application.Services;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.UnitTests;

public sealed class ScheduledPublicationServiceTests
{
    [Fact]
    public async Task Promote_due_marks_scheduled_artworks_published_with_one_commit()
    {
        var now = new DateTime(2026, 9, 29, 18, 0, 0, DateTimeKind.Utc);
        var first = Scheduled(now.AddMinutes(-5));
        var second = Scheduled(now.AddSeconds(-1));
        var future = Scheduled(now.AddMinutes(5));
        var repo = new FakeArtworkRepository(first, second, future);
        var uow = new FakeUnitOfWork();
        var sut = new ScheduledPublicationService(repo, uow);

        var count = await sut.PromoteDueAsync(now, CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal(1, uow.SaveCalls);
        Assert.Equal(PublishingStatus.Published, first.PublishingStatus);
        Assert.Equal(PublishingStatus.Published, second.PublishingStatus);
        Assert.Equal(now, first.PublishedAtUtc);
        Assert.Equal(now, second.PublishedAtUtc);
        Assert.Equal(PublishingStatus.Scheduled, future.PublishingStatus);
        Assert.Null(future.PublishedAtUtc);
    }

    [Fact]
    public async Task Promote_due_does_not_commit_when_nothing_is_due()
    {
        var now = DateTime.UtcNow;
        var repo = new FakeArtworkRepository(Scheduled(now.AddHours(1)));
        var uow = new FakeUnitOfWork();
        var sut = new ScheduledPublicationService(repo, uow);

        var count = await sut.PromoteDueAsync(now, CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Equal(0, uow.SaveCalls);
    }

    private static Artwork Scheduled(DateTime when) => new()
    {
        Title = "Scheduled",
        CategoryId = Guid.NewGuid(),
        LicenseType = "Original",
        Difficulty = 3,
        RegionCount = 120,
        PublishingStatus = PublishingStatus.Scheduled,
        ScheduledPublishAtUtc = when
    };

    private sealed class FakeArtworkRepository(params Artwork[] seed) : IArtworkRepository
    {
        public List<Artwork> Items { get; } = [.. seed];

        public Task<IReadOnlyCollection<Artwork>> ListDueScheduledAsync(DateTime utcNow, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<Artwork>>(
                Items.Where(x => x.PublishingStatus == PublishingStatus.Scheduled
                    && x.ScheduledPublishAtUtc <= utcNow)
                    .OrderBy(x => x.ScheduledPublishAtUtc)
                    .Take(take)
                    .ToArray());

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
