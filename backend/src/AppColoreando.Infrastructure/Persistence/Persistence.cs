using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppColoreando.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<LicenseAgreement> LicenseAgreements => Set<LicenseAgreement>();
    public DbSet<LicenseTerritory> LicenseTerritories => Set<LicenseTerritory>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionArtwork> CollectionArtworks => Set<CollectionArtwork>();
    public DbSet<Artwork> Artworks => Set<Artwork>();
    public DbSet<ArtworkAsset> ArtworkAssets => Set<ArtworkAsset>();
    public DbSet<UserArtworkProgress> UserArtworkProgress => Set<UserArtworkProgress>();
    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<UserActivityHistory> UserActivityHistory => Set<UserActivityHistory>();
    public DbSet<UserMetricDaily> UserMetricsDaily => Set<UserMetricDaily>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<UserProfile>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<RefreshToken>().HasIndex(x => new { x.UserId, x.DeviceId });
        b.Entity<Country>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Brand>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Collection>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<CollectionArtwork>().HasKey(x => new { x.CollectionId, x.ArtworkId });
        b.Entity<CollectionArtwork>().HasIndex(x => new { x.CollectionId, x.SortOrder });
        b.Entity<Artwork>().HasIndex(x => new { x.CategoryId, x.PublishingStatus });
        b.Entity<Artwork>().HasIndex(x => new { x.CountryCode, x.PublishingStatus });
        b.Entity<Artwork>().HasMany(x => x.Assets).WithOne().HasForeignKey(x => x.ArtworkId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LicenseAgreement>().HasMany(x => x.Territories).WithOne().HasForeignKey(x => x.LicenseAgreementId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<UserArtworkProgress>().HasIndex(x => new { x.UserId, x.ArtworkId }).IsUnique();
        b.Entity<UserArtworkProgress>().Property(x => x.CompletionPercent).HasPrecision(5, 2);
        b.Entity<SyncOperation>().HasIndex(x => new { x.UserId, x.ClientOperationId }).IsUnique();
        b.Entity<UserAchievement>().HasIndex(x => new { x.UserId, x.Code }).IsUnique();
        b.Entity<UserMetricDaily>().HasIndex(x => new { x.UserId, x.MetricDate }).IsUnique();
        b.Entity<Artwork>().Property(x => x.PublishingStatus).HasConversion<string>();
        b.Entity<ArtworkAsset>().Property(x => x.Kind).HasConversion<string>();
        b.Entity<SyncOperation>().Property(x => x.Status).HasConversion<string>();
    }
}

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) => db.Users.AnyAsync(x => x.Email == email, ct);
    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct) => db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct) => db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken ct) => await db.Users.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Select(x => new UserDto(x.Id, x.Email, x.DisplayName, x.Role, x.IsActive, x.CreatedAtUtc, x.LastLoginAtUtc)).ToArrayAsync(ct);
    public async Task AddAsync(AppUser user, CancellationToken ct) => await db.Users.AddAsync(user, ct);
}

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct) => db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
    public async Task<IReadOnlyCollection<UserSessionDto>> ListSessionsAsync(Guid userId, CancellationToken ct) => await db.RefreshTokens.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new UserSessionDto(x.Id, x.DeviceId, x.DeviceName, x.ExpiresAtUtc, x.RevokedAtUtc, x.CreatedAtUtc)).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<RefreshToken>> ListActiveAsync(Guid userId, CancellationToken ct) => await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).ToArrayAsync(ct);
    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);
}

public sealed class UserProfileRepository(AppDbContext db) : IUserProfileRepository
{
    public Task<UserProfile?> GetAsync(Guid userId, CancellationToken ct) => db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct);
    public void Add(UserProfile profile) => db.UserProfiles.Add(profile);
}

public sealed class ArtworkRepository(AppDbContext db) : IArtworkRepository
{
    public Task<bool> PublishedExistsAsync(Guid id, CancellationToken ct) => db.Artworks.AnyAsync(x => x.Id == id && x.PublishingStatus == PublishingStatus.Published, ct);
    public Task<Artwork?> GetAsync(Guid id, CancellationToken ct) => db.Artworks.Include(x => x.Assets).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<PageResult<ArtworkDto>> SearchAsync(CatalogQuery q, CancellationToken ct)
    {
        var query = db.Artworks.AsNoTracking().Include(x => x.Assets).Where(x => x.PublishingStatus == PublishingStatus.Published);
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(x => x.Title.ToLower().Contains(q.Search.ToLower()) || (x.Description != null && x.Description.ToLower().Contains(q.Search.ToLower())));
        if (!string.IsNullOrWhiteSpace(q.CountryCode)) query = query.Where(x => x.CountryCode == q.CountryCode.ToUpper());
        if (q.CategoryId.HasValue) query = query.Where(x => x.CategoryId == q.CategoryId.Value);
        if (q.Difficulty.HasValue) query = query.Where(x => x.Difficulty == q.Difficulty.Value);
        if (q.LicensedOnly.HasValue) query = query.Where(x => q.LicensedOnly.Value ? x.LicenseAgreementId != null : x.LicenseAgreementId == null);
        if (q.CollectionId.HasValue)
        {
            var ids = db.CollectionArtworks.Where(x => x.CollectionId == q.CollectionId.Value).Select(x => x.ArtworkId);
            query = query.Where(x => ids.Contains(x.Id));
        }
        var total = await query.CountAsync(ct);
        var entities = await query.OrderByDescending(x => x.PublishedAtUtc).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToArrayAsync(ct);
        return new PageResult<ArtworkDto>(entities.Select(MapArtwork).ToArray(), q.Page, q.PageSize, total);
    }
    public async Task AddAsync(Artwork artwork, CancellationToken ct) => await db.Artworks.AddAsync(artwork, ct);
    public Task<int> CountPublishedAsync(CancellationToken ct) => db.Artworks.CountAsync(x => x.PublishingStatus == PublishingStatus.Published, ct);
    internal static ArtworkDto MapArtwork(Artwork x) => new(x.Id, x.Title, x.Description, x.CategoryId, x.CountryCode, x.BrandId, x.LicenseAgreementId, x.LicenseType, x.LicenseReference, x.ThumbnailUrl, x.AssetUrl, x.BundleChecksum, x.Difficulty, x.RegionCount, x.PublishingStatus, x.ScheduledPublishAtUtc, x.PublishedAtUtc, x.Assets.Select(a => new ArtworkAssetDto(a.Id, a.Kind, a.Uri, a.ContentType, a.Checksum, a.SizeBytes, a.IsPrimary)).ToArray());
}

public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<Category?> GetAsync(Guid id, CancellationToken ct) => db.Categories.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<CategoryDto>> ListAsync(CancellationToken ct) => await db.Categories.AsNoTracking().OrderBy(x => x.Name).Select(x => new CategoryDto(x.Id, x.Name, x.Slug, x.IsActive)).ToArrayAsync(ct);
    public async Task AddAsync(Category category, CancellationToken ct) => await db.Categories.AddAsync(category, ct);
}

public sealed class CountryRepository(AppDbContext db) : ICountryRepository
{
    public async Task<IReadOnlyCollection<CountryDto>> ListAsync(CancellationToken ct) => await db.Countries.AsNoTracking().OrderBy(x => x.Name).Select(x => new CountryDto(x.Id, x.Code, x.Name, x.IsActive)).ToArrayAsync(ct);
    public Task<Country?> GetByCodeAsync(string code, CancellationToken ct) => db.Countries.SingleOrDefaultAsync(x => x.Code == code, ct);
    public void Add(Country country) => db.Countries.Add(country);
}

public sealed class BrandRepository(AppDbContext db) : IBrandRepository
{
    public async Task<IReadOnlyCollection<BrandDto>> ListAsync(CancellationToken ct) => await db.Brands.AsNoTracking().OrderBy(x => x.Name).Select(x => new BrandDto(x.Id, x.Name, x.Slug, x.Enabled, x.Notes)).ToArrayAsync(ct);
    public Task<Brand?> GetAsync(Guid id, CancellationToken ct) => db.Brands.SingleOrDefaultAsync(x => x.Id == id, ct);
    public void Add(Brand brand) => db.Brands.Add(brand);
}

public sealed class LicenseRepository(AppDbContext db) : ILicenseRepository
{
    public async Task<IReadOnlyCollection<LicenseAgreementDto>> ListAsync(CancellationToken ct)
    {
        var entities = await db.LicenseAgreements.AsNoTracking().Include(x => x.Territories).OrderBy(x => x.Name).ToArrayAsync(ct);
        return entities.Select(Map).ToArray();
    }
    public Task<LicenseAgreement?> GetAsync(Guid id, CancellationToken ct) => db.LicenseAgreements.Include(x => x.Territories).SingleOrDefaultAsync(x => x.Id == id, ct);
    public void Add(LicenseAgreement license) => db.LicenseAgreements.Add(license);
    internal static LicenseAgreementDto Map(LicenseAgreement x) => new(x.Id, x.BrandId, x.Name, x.RightsSummary, x.ValidFromUtc, x.ValidToUtc, x.AllowsStoreDistribution, x.AllowsKidsMode, x.AllowsOfflineDownload, x.FeatureEnabled, x.Territories.Select(t => new LicenseTerritoryDto(t.Id, t.CountryCode, t.IsAllowed)).ToArray());
}

public sealed class CollectionRepository(AppDbContext db) : ICollectionRepository
{
    public async Task<IReadOnlyCollection<CollectionDto>> ListAsync(CancellationToken ct) => await db.Collections.AsNoTracking().Include(x => x.Artworks).OrderBy(x => x.Name).Select(x => new CollectionDto(x.Id, x.Name, x.Slug, x.Description, x.CountryCode, x.IsActive, x.Artworks.Count)).ToArrayAsync(ct);
    public Task<Collection?> GetAsync(Guid id, CancellationToken ct) => db.Collections.Include(x => x.Artworks).SingleOrDefaultAsync(x => x.Id == id, ct);
    public void Add(Collection collection) => db.Collections.Add(collection);
}

public sealed class UserProgressRepository(AppDbContext db) : IUserProgressRepository
{
    public Task<UserArtworkProgress?> GetAsync(Guid userId, Guid artworkId, CancellationToken ct) => db.UserArtworkProgress.SingleOrDefaultAsync(x => x.UserId == userId && x.ArtworkId == artworkId, ct);
    public async Task<IReadOnlyCollection<UserArtworkProgress>> ListAsync(Guid userId, CancellationToken ct) => await db.UserArtworkProgress.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.LastOpenedAtUtc).ToArrayAsync(ct);
    public Task<int> CountCompletedAsync(CancellationToken ct) => db.UserArtworkProgress.CountAsync(x => x.CompletedAtUtc != null, ct);
    public void Add(UserArtworkProgress progress) => db.UserArtworkProgress.Add(progress);
}

public sealed class SyncOperationRepository(AppDbContext db) : ISyncOperationRepository
{
    public Task<bool> ExistsAsync(Guid userId, string clientOperationId, CancellationToken ct) => db.SyncOperations.AnyAsync(x => x.UserId == userId && x.ClientOperationId == clientOperationId, ct);
    public void Add(SyncOperation operation) => db.SyncOperations.Add(operation);
}

public sealed class UserAchievementRepository(AppDbContext db) : IUserAchievementRepository
{
    public async Task<IReadOnlyCollection<UserAchievement>> ListAsync(Guid userId, CancellationToken ct) => await db.UserAchievements.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.EarnedAtUtc).ToArrayAsync(ct);
    public Task<bool> ExistsAsync(Guid userId, string code, CancellationToken ct) => db.UserAchievements.AnyAsync(x => x.UserId == userId && x.Code == code, ct);
    public void Add(UserAchievement achievement) => db.UserAchievements.Add(achievement);
}

public sealed class UserActivityRepository(AppDbContext db) : IUserActivityRepository
{
    public void Add(UserActivityHistory activity) => db.UserActivityHistory.Add(activity);
    public async Task<IReadOnlyCollection<UserActivityDto>> ListAsync(Guid userId, int take, CancellationToken ct) => await db.UserActivityHistory.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.OccurredAtUtc).Take(take).Select(x => new UserActivityDto(x.Id, x.ArtworkId, x.ActivityType, x.MetadataJson, x.OccurredAtUtc)).ToArrayAsync(ct);
    public Task<int> CountAsync(CancellationToken ct) => db.UserActivityHistory.CountAsync(ct);
}

public sealed class UserMetricRepository(AppDbContext db) : IUserMetricRepository
{
    public Task<UserMetricDaily?> GetAsync(Guid userId, DateOnly date, CancellationToken ct) => db.UserMetricsDaily.SingleOrDefaultAsync(x => x.UserId == userId && x.MetricDate == date, ct);
    public void Add(UserMetricDaily metric) => db.UserMetricsDaily.Add(metric);
    public async Task<IReadOnlyCollection<UserMetricDaily>> ListAsync(Guid userId, int days, CancellationToken ct) => await db.UserMetricsDaily.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.MetricDate).Take(days).ToArrayAsync(ct);
}

public sealed class AuditRepository(AppDbContext db) : IAuditRepository
{
    public void Add(AuditLog audit) => db.AuditLogs.Add(audit);
    public async Task<IReadOnlyCollection<AuditLogDto>> ListAsync(int take, CancellationToken ct) => await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(take).Select(x => new AuditLogDto(x.Id, x.UserId, x.EntityName, x.EntityId, x.Action, x.ChangesJson, x.OccurredAtUtc)).ToArrayAsync(ct);
}
