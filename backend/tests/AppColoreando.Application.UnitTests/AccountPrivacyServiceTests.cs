using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Application.Services;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.UnitTests;

public sealed class AccountPrivacyServiceTests
{
    [Fact]
    public async Task Delete_removes_regular_user_personal_data()
    {
        var user = new AppUser { Id = Guid.NewGuid(), Email = "user@example.com", DisplayName = "User", Role = AppRole.User.ToString() };
        var privacy = new FakePrivacyRepository();
        var sut = CreateService(user, privacy);

        await sut.DeleteAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, privacy.DeletedUserId);
    }

    [Fact]
    public async Task Delete_rejects_privileged_account()
    {
        var admin = new AppUser { Id = Guid.NewGuid(), Email = "admin@example.com", DisplayName = "Admin", Role = AppRole.Admin.ToString() };
        var privacy = new FakePrivacyRepository();
        var sut = CreateService(admin, privacy);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteAsync(admin.Id, CancellationToken.None));
        Assert.Null(privacy.DeletedUserId);
    }

    [Fact]
    public async Task Export_contains_user_profile_and_entitlements()
    {
        var user = new AppUser { Id = Guid.NewGuid(), Email = "user@example.com", DisplayName = "User", Role = AppRole.User.ToString() };
        var sut = CreateService(user, new FakePrivacyRepository());

        var export = await sut.ExportAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, export.User.Id);
        Assert.Equal("es", export.Profile.Locale);
        Assert.Equal("free", export.Entitlements.PlanCode);
        Assert.True(export.ExportedAtUtc <= DateTime.UtcNow);
    }

    private static AccountPrivacyService CreateService(AppUser user, FakePrivacyRepository privacy) =>
        new(new FakeUserRepository(user), new FakeAccountService(user), new FakeContentService(), new FakeActivityRepository(), new FakeMetricRepository(), privacy);

    private sealed class FakeUserRepository(AppUser user) : IUserRepository
    {
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) => Task.FromResult(email == user.Email);
        public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct) => Task.FromResult<AppUser?>(email == user.Email ? user : null);
        public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<AppUser?>(id == user.Id ? user : null);
        public Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<UserDto>>([]);
        public Task AddAsync(AppUser value, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeAccountService(AppUser user) : IUserAccountService
    {
        public Task<(UserDto User, UserProfileDto Profile)> GetProfileAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult((new UserDto(user.Id, user.Email, user.DisplayName, user.Role, user.IsActive, user.CreatedAtUtc, user.LastLoginAtUtc), new UserProfileDto("es", false, true, true, "Any", "System")));

        public Task<(UserDto User, UserProfileDto Profile)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<UserSessionDto>> GetSessionsAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<UserSessionDto>>([]);
    }

    private sealed class FakeContentService : IUserContentService
    {
        public Task<IReadOnlyCollection<ProgressDto>> GetProgressAsync(Guid userId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ProgressDto>>([]);
        public Task<SyncProgressResponse> SaveProgressAsync(Guid userId, Guid artworkId, SaveProgressRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserLibraryResponse> GetLibraryAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult(new UserLibraryResponse([], [], [], [], []));
        public Task<IReadOnlyCollection<UserActivityDto>> GetHistoryAsync(Guid userId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<UserActivityDto>>([]);
        public Task<UserMetricsResponse> GetMetricsAsync(Guid userId, CancellationToken ct) => Task.FromResult(new UserMetricsResponse([], 0, 0, 0, 0, 0));
        public Task<UserAchievementSummary> GetAchievementsAsync(Guid userId, CancellationToken ct) => Task.FromResult(new UserAchievementSummary(0, 1, 0, 100, 0, []));
        public Task<TelemetryIngestResponse> RecordTelemetryAsync(Guid userId, ClientTelemetryBatchRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<MonetizationEntitlementsResponse> GetEntitlementsAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult(new MonetizationEntitlementsResponse("free", false, true, 8, false, false, false));
    }


    private sealed class FakeActivityRepository : IUserActivityRepository
    {
        public void Add(UserActivityHistory activity) { }
        public Task<IReadOnlyCollection<UserActivityDto>> ListAsync(Guid userId, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<UserActivityDto>>([]);
        public Task<int> CountAsync(CancellationToken ct) => Task.FromResult(0);
    }

    private sealed class FakeMetricRepository : IUserMetricRepository
    {
        public Task<UserMetricDaily?> GetAsync(Guid userId, DateOnly date, CancellationToken ct) =>
            Task.FromResult<UserMetricDaily?>(null);
        public void Add(UserMetricDaily metric) { }
        public Task<IReadOnlyCollection<UserMetricDaily>> ListAsync(Guid userId, int days, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<UserMetricDaily>>([]);
    }

    private sealed class FakePrivacyRepository : IUserPrivacyRepository
    {
        public Guid? DeletedUserId { get; private set; }
        public Task DeletePersonalDataAsync(Guid userId, CancellationToken ct)
        {
            DeletedUserId = userId;
            return Task.CompletedTask;
        }
    }
}
