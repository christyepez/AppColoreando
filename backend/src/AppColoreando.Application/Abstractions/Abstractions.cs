using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken ct);
    Task AddAsync(AppUser user, CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct);
    Task<IReadOnlyCollection<UserSessionDto>> ListSessionsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyCollection<RefreshToken>> ListActiveAsync(Guid userId, CancellationToken ct);
    void Add(RefreshToken token);
}

public interface IUserProfileRepository
{
    Task<UserProfile?> GetAsync(Guid userId, CancellationToken ct);
    void Add(UserProfile profile);
}

public interface IArtworkRepository
{
    Task<bool> PublishedExistsAsync(Guid id, CancellationToken ct);
    Task<Artwork?> GetAsync(Guid id, CancellationToken ct);
    Task<PageResult<ArtworkDto>> SearchAsync(CatalogQuery query, CancellationToken ct);
    Task AddAsync(Artwork artwork, CancellationToken ct);
    Task<int> CountPublishedAsync(CancellationToken ct);
}

public interface ICategoryRepository
{
    Task<Category?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<CategoryDto>> ListAsync(CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
}

public interface ICountryRepository
{
    Task<IReadOnlyCollection<CountryDto>> ListAsync(CancellationToken ct);
    Task<Country?> GetByCodeAsync(string code, CancellationToken ct);
    void Add(Country country);
}

public interface IBrandRepository
{
    Task<IReadOnlyCollection<BrandDto>> ListAsync(CancellationToken ct);
    Task<Brand?> GetAsync(Guid id, CancellationToken ct);
    void Add(Brand brand);
}

public interface ILicenseRepository
{
    Task<IReadOnlyCollection<LicenseAgreementDto>> ListAsync(CancellationToken ct);
    Task<LicenseAgreement?> GetAsync(Guid id, CancellationToken ct);
    void Add(LicenseAgreement license);
}

public interface ICollectionRepository
{
    Task<IReadOnlyCollection<CollectionDto>> ListAsync(CancellationToken ct);
    Task<Collection?> GetAsync(Guid id, CancellationToken ct);
    void Add(Collection collection);
}

public interface IUserProgressRepository
{
    Task<UserArtworkProgress?> GetAsync(Guid userId, Guid artworkId, CancellationToken ct);
    Task<IReadOnlyCollection<UserArtworkProgress>> ListAsync(Guid userId, CancellationToken ct);
    Task<int> CountCompletedAsync(CancellationToken ct);
    void Add(UserArtworkProgress progress);
}

public interface ISyncOperationRepository
{
    Task<bool> ExistsAsync(Guid userId, string clientOperationId, CancellationToken ct);
    void Add(SyncOperation operation);
}

public interface IUserAchievementRepository
{
    Task<IReadOnlyCollection<UserAchievement>> ListAsync(Guid userId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid userId, string code, CancellationToken ct);
    void Add(UserAchievement achievement);
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
public interface ITokenService { (string Token, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user); string CreateRefreshToken(); string HashRefreshToken(string token); }
public interface ICurrentUser { Guid UserId { get; } }
public interface IArtworkBundleProcessor { BundleValidationResult Validate(string bundleJson); }

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct);
    Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken ct);
    Task LogoutAsync(RevokeTokenRequest request, CancellationToken ct);
    Task RevokeAllAsync(Guid userId, CancellationToken ct);
}

public interface IUserAccountService
{
    Task<(UserDto User, UserProfileDto Profile)> GetProfileAsync(Guid userId, CancellationToken ct);
    Task<(UserDto User, UserProfileDto Profile)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<UserSessionDto>> GetSessionsAsync(Guid userId, CancellationToken ct);
}

public interface ICatalogService
{
    Task<PageResult<ArtworkDto>> SearchAsync(CatalogQuery query, CancellationToken ct);
    Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CollectionDto>> GetCollectionsAsync(CancellationToken ct);
}

public interface IUserContentService
{
    Task<IReadOnlyCollection<ProgressDto>> GetProgressAsync(Guid userId, CancellationToken ct);
    Task<SyncProgressResponse> SaveProgressAsync(Guid userId, Guid artworkId, SaveProgressRequest request, CancellationToken ct);
    Task<UserLibraryResponse> GetLibraryAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyCollection<UserActivityDto>> GetHistoryAsync(Guid userId, CancellationToken ct);
    Task<UserMetricsResponse> GetMetricsAsync(Guid userId, CancellationToken ct);
    Task<UserAchievementSummary> GetAchievementsAsync(Guid userId, CancellationToken ct);
}

public interface IAdminService
{
    Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryRequest request, CancellationToken ct);
    Task<CountryDto> UpsertCountryAsync(Guid userId, UpsertCountryRequest request, CancellationToken ct);
    Task<BrandDto> UpsertBrandAsync(Guid userId, Guid? id, UpsertBrandRequest request, CancellationToken ct);
    Task<LicenseAgreementDto> UpsertLicenseAsync(Guid userId, Guid? id, UpsertLicenseAgreementRequest request, CancellationToken ct);
    Task<CollectionDto> UpsertCollectionAsync(Guid userId, Guid? id, UpsertCollectionRequest request, CancellationToken ct);
    Task<ArtworkDto> CreateArtworkAsync(Guid userId, UpsertArtworkRequest request, CancellationToken ct);
    Task<ArtworkDto?> UpdateArtworkAsync(Guid userId, Guid id, UpsertArtworkRequest request, CancellationToken ct);
    Task<UserDto?> UpdateUserAsync(Guid userId, Guid targetUserId, AdminUpdateUserRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<BrandDto>> GetBrandsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<LicenseAgreementDto>> GetLicensesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken ct);
    Task<IReadOnlyCollection<AuditLogDto>> GetAuditAsync(CancellationToken ct);
    Task<AdminMetricsResponse> GetMetricsAsync(CancellationToken ct);
}
