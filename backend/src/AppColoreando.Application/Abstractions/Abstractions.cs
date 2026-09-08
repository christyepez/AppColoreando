using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct);
    Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken ct);
    Task AddAsync(AppUser user, CancellationToken ct);
}

public interface IArtworkRepository
{
    Task<bool> PublishedExistsAsync(Guid id, CancellationToken ct);
    Task<Artwork?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<ArtworkDto>> ListPublishedAsync(CancellationToken ct);
    Task AddAsync(Artwork artwork, CancellationToken ct);
    Task<int> CountPublishedAsync(CancellationToken ct);
}

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken ct);
}

public interface IUserProgressRepository
{
    Task<UserArtworkProgress?> GetAsync(Guid userId, Guid artworkId, CancellationToken ct);
    Task<IReadOnlyCollection<UserArtworkProgress>> ListAsync(Guid userId, CancellationToken ct);
    Task<int> CountCompletedAsync(CancellationToken ct);
    void Add(UserArtworkProgress progress);
}

public interface IUserActivityRepository
{
    void Add(UserActivityHistory activity);
    Task<IReadOnlyCollection<UserActivityDto>> ListAsync(Guid userId, int take, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
}

public interface IUserMetricRepository
{
    Task<UserMetricDaily?> GetAsync(Guid userId, DateOnly date, CancellationToken ct);
    void Add(UserMetricDaily metric);
    Task<IReadOnlyCollection<UserMetricDaily>> ListAsync(Guid userId, int days, CancellationToken ct);
}

public interface IAuditRepository
{
    void Add(AuditLog audit);
    Task<IReadOnlyCollection<AuditLogDto>> ListAsync(int take, CancellationToken ct);
}

public interface IUnitOfWork { Task<int> SaveChangesAsync(CancellationToken ct); }
public interface IPasswordService { string Hash(AppUser user, string password); bool Verify(AppUser user, string hash, string password); }
public interface ITokenService { string Create(AppUser user); }
public interface ICurrentUser { Guid UserId { get; } }

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
}
public interface ICatalogService { Task<IReadOnlyCollection<ArtworkDto>> GetPublishedAsync(CancellationToken ct); }
public interface IUserContentService
{
    Task<IReadOnlyCollection<ProgressDto>> GetProgressAsync(Guid userId, CancellationToken ct);
    Task<ProgressDto> SaveProgressAsync(Guid userId, Guid artworkId, SaveProgressRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<UserActivityDto>> GetHistoryAsync(Guid userId, CancellationToken ct);
    Task<UserMetricsResponse> GetMetricsAsync(Guid userId, CancellationToken ct);
}
public interface IAdminService
{
    Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryRequest request, CancellationToken ct);
    Task<ArtworkDto> CreateArtworkAsync(Guid userId, CreateArtworkRequest request, CancellationToken ct);
    Task<ArtworkDto?> UpdateArtworkAsync(Guid userId, Guid id, UpdateArtworkRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken ct);
    Task<IReadOnlyCollection<AuditLogDto>> GetAuditAsync(CancellationToken ct);
    Task<AdminMetricsResponse> GetMetricsAsync(CancellationToken ct);
}
