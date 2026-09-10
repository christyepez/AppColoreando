using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Application.Services;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.UnitTests;

public sealed class AuthServiceSecurityTests
{
    [Fact]
    public async Task Refresh_replay_revokes_all_active_sessions()
    {
        var userId = Guid.NewGuid();
        var replayed = new RefreshToken { UserId = userId, TokenHash = "hash:old", DeviceId = "phone", DeviceName = "Phone", ExpiresAtUtc = DateTime.UtcNow.AddDays(1), RevokedAtUtc = DateTime.UtcNow.AddMinutes(-1), ReplacedByTokenHash = "hash:new" };
        var active = new RefreshToken { UserId = userId, TokenHash = "hash:active", DeviceId = "tablet", DeviceName = "Tablet", ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        var tokenRepo = new FakeRefreshTokenRepository(replayed, [active]);
        var activities = new FakeActivityRepository();
        var uow = new FakeUnitOfWork();
        var service = CreateService(tokenRepo, activities, uow);

        var result = await service.RefreshAsync(new RefreshTokenRequest("old", "phone"), CancellationToken.None);

        Assert.Null(result);
        Assert.NotNull(active.RevokedAtUtc);
        Assert.Contains(activities.Items, x => x.ActivityType == "RefreshTokenReplayDetected");
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Refresh_device_mismatch_is_rejected_and_audited()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken { UserId = userId, TokenHash = "hash:token", DeviceId = "phone", DeviceName = "Phone", ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        var tokenRepo = new FakeRefreshTokenRepository(token, [token]);
        var activities = new FakeActivityRepository();
        var uow = new FakeUnitOfWork();
        var service = CreateService(tokenRepo, activities, uow);

        var result = await service.RefreshAsync(new RefreshTokenRequest("token", "other-device"), CancellationToken.None);

        Assert.Null(result);
        Assert.Null(token.RevokedAtUtc);
        Assert.Contains(activities.Items, x => x.ActivityType == "RefreshTokenDeviceMismatch");
        Assert.Equal(1, uow.SaveCount);
    }

    private static AuthService CreateService(FakeRefreshTokenRepository tokens, FakeActivityRepository activities, FakeUnitOfWork uow) =>
        new(new FakeUserRepository(), new FakeProfileRepository(), tokens, activities, new FakeMetricRepository(), new FakePasswordService(), new FakeTokenService(), uow);

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) => Task.FromResult(false);
        public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct) => Task.FromResult<AppUser?>(null);
        public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<AppUser?>(new AppUser { Id = id, Email = "user@example.com", DisplayName = "User" });
        public Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<UserDto>>([]);
        public Task AddAsync(AppUser user, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeProfileRepository : IUserProfileRepository
    {
        public Task<UserProfile?> GetAsync(Guid userId, CancellationToken ct) => Task.FromResult<UserProfile?>(null);
        public void Add(UserProfile profile) { }
    }

    private sealed class FakeRefreshTokenRepository(RefreshToken token, IReadOnlyCollection<RefreshToken> active) : IRefreshTokenRepository
    {
        public readonly List<RefreshToken> Added = [];
        public Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct) => Task.FromResult<RefreshToken?>(hash == token.TokenHash ? token : null);
        public Task<IReadOnlyCollection<UserSessionDto>> ListSessionsAsync(Guid userId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<UserSessionDto>>([]);
        public Task<IReadOnlyCollection<RefreshToken>> ListActiveAsync(Guid userId, CancellationToken ct) => Task.FromResult(active);
        public void Add(RefreshToken value) => Added.Add(value);
    }

    private sealed class FakeActivityRepository : IUserActivityRepository
    {
        public readonly List<UserActivityHistory> Items = [];
        public void Add(UserActivityHistory activity) => Items.Add(activity);
        public Task<IReadOnlyCollection<UserActivityDto>> ListAsync(Guid userId, int take, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<UserActivityDto>>([]);
        public Task<int> CountAsync(CancellationToken ct) => Task.FromResult(Items.Count);
    }

    private sealed class FakeMetricRepository : IUserMetricRepository
    {
        public Task<UserMetricDaily?> GetAsync(Guid userId, DateOnly date, CancellationToken ct) => Task.FromResult<UserMetricDaily?>(null);
        public void Add(UserMetricDaily metric) { }
        public Task<IReadOnlyCollection<UserMetricDaily>> ListAsync(Guid userId, int days, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<UserMetricDaily>>([]);
    }

    private sealed class FakePasswordService : IPasswordService
    {
        public string Hash(AppUser user, string password) => password;
        public bool Verify(AppUser user, string hash, string password) => true;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user) => ("access", DateTime.UtcNow.AddMinutes(30));
        public string CreateRefreshToken() => "new-refresh";
        public string HashRefreshToken(string token) => $"hash:{token}";
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken ct) { SaveCount++; return Task.FromResult(1); }
    }
}
