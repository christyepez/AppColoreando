using System.Text.Json;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Services;

public sealed class AuthService(IUserRepository users, IUserActivityRepository activities, IUserMetricRepository metrics, IPasswordService passwords, ITokenService tokens, IUnitOfWork uow) : IAuthService
{
    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new ArgumentException("Invalid email.");
        if (request.Password.Length < 10) throw new ArgumentException("Password must contain at least 10 characters.");
        if (string.IsNullOrWhiteSpace(request.DisplayName)) throw new ArgumentException("DisplayName is required.");
        if (await users.ExistsByEmailAsync(email, ct)) throw new InvalidOperationException("Email already registered.");
        var user = new AppUser { Email = email, DisplayName = request.DisplayName.Trim() };
        user.PasswordHash = passwords.Hash(user, request.Password);
        await users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return ToUser(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !user.IsActive || !passwords.Verify(user, user.PasswordHash, request.Password)) return null;
        user.LastLoginAtUtc = DateTime.UtcNow;
        activities.Add(new UserActivityHistory { UserId = user.Id, ActivityType = "Login" });
        var metric = await GetOrCreateMetric(metrics, user.Id, ct); metric.Sessions++;
        await uow.SaveChangesAsync(ct);
        return new AuthResponse(tokens.Create(user), ToUser(user));
    }

    internal static UserDto ToUser(AppUser x) => new(x.Id, x.Email, x.DisplayName, x.Role, x.IsActive, x.CreatedAtUtc, x.LastLoginAtUtc);
    internal static async Task<UserMetricDaily> GetOrCreateMetric(IUserMetricRepository repo, Guid userId, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var metric = await repo.GetAsync(userId, date, ct);
        if (metric is not null) return metric;
        metric = new UserMetricDaily { UserId = userId, MetricDate = date }; repo.Add(metric); return metric;
    }
}

public sealed class CatalogService(IArtworkRepository artworks) : ICatalogService
{
    public Task<IReadOnlyCollection<ArtworkDto>> GetPublishedAsync(CancellationToken ct) => artworks.ListPublishedAsync(ct);
}

public sealed class UserContentService(IArtworkRepository artworks, IUserProgressRepository progress, IUserActivityRepository activities, IUserMetricRepository metrics, IUnitOfWork uow) : IUserContentService
{
    public async Task<IReadOnlyCollection<ProgressDto>> GetProgressAsync(Guid userId, CancellationToken ct) => (await progress.ListAsync(userId, ct)).Select(Map).ToArray();

    public async Task<ProgressDto> SaveProgressAsync(Guid userId, Guid artworkId, SaveProgressRequest request, CancellationToken ct)
    {
        if (!await artworks.PublishedExistsAsync(artworkId, ct)) throw new KeyNotFoundException("Artwork not found.");
        var item = await progress.GetAsync(userId, artworkId, ct);
        if (item is null) { item = new UserArtworkProgress { UserId = userId, ArtworkId = artworkId }; progress.Add(item); }
        var distinctRegions = request.CompletedRegionIds.Distinct().ToArray();
        item.CompletionPercent = Math.Clamp(request.CompletionPercent, 0, 100);
        item.CompletedRegionIdsJson = JsonSerializer.Serialize(distinctRegions);
        item.LastOpenedAtUtc = DateTime.UtcNow;
        item.IsFavorite = request.IsFavorite;
        var completedNow = item.CompletionPercent >= 100 && item.CompletedAtUtc is null;
        if (completedNow) item.CompletedAtUtc = DateTime.UtcNow;
        activities.Add(new UserActivityHistory { UserId = userId, ArtworkId = artworkId, ActivityType = completedNow ? "ArtworkCompleted" : "ProgressSaved", MetadataJson = JsonSerializer.Serialize(new { item.CompletionPercent, regionCount = distinctRegions.Length }) });
        var metric = await AuthService.GetOrCreateMetric(metrics, userId, ct);
        metric.ArtworksOpened++;
        metric.RegionsColored += distinctRegions.Length;
        if (completedNow) metric.ArtworksCompleted++;
        await uow.SaveChangesAsync(ct);
        return Map(item);
    }

    public Task<IReadOnlyCollection<UserActivityDto>> GetHistoryAsync(Guid userId, CancellationToken ct) => activities.ListAsync(userId, 250, ct);

    public async Task<UserMetricsResponse> GetMetricsAsync(Guid userId, CancellationToken ct)
    {
        var data = await metrics.ListAsync(userId, 90, ct);
        var daily = data.Select(x => new DailyMetricDto(x.MetricDate, x.Sessions, x.ArtworksOpened, x.ArtworksCompleted, x.RegionsColored, x.ActiveSeconds)).ToArray();
        return new UserMetricsResponse(daily, data.Sum(x => x.Sessions), data.Sum(x => x.ArtworksOpened), data.Sum(x => x.ArtworksCompleted), data.Sum(x => x.RegionsColored), data.Sum(x => x.ActiveSeconds));
    }

    private static ProgressDto Map(UserArtworkProgress x)
    {
        var ids = JsonSerializer.Deserialize<int[]>(x.CompletedRegionIdsJson) ?? [];
        return new ProgressDto(x.ArtworkId, x.CompletionPercent, ids, x.IsFavorite, x.LastOpenedAtUtc, x.CompletedAtUtc);
    }
}

public sealed class AdminService(ICategoryRepository categories, IArtworkRepository artworks, IUserRepository users, IUserProgressRepository progress, IUserActivityRepository activities, IAuditRepository audit, IUnitOfWork uow) : IAdminService
{
    public async Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Slug)) throw new ArgumentException("Name and Slug are required.");
        var entity = new Category { Name = r.Name.Trim(), Slug = r.Slug.Trim().ToLowerInvariant(), IsActive = r.IsActive, CreatedByUserId = userId };
        await categories.AddAsync(entity, ct); audit.Add(Audit(userId, "Category", entity.Id, "Create", r)); await uow.SaveChangesAsync(ct);
        return new(entity.Id, entity.Name, entity.Slug, entity.IsActive);
    }

    public async Task<ArtworkDto> CreateArtworkAsync(Guid userId, CreateArtworkRequest r, CancellationToken ct)
    {
        ValidateArtwork(r.Title, r.Difficulty, r.RegionCount);
        var e = new Artwork { Title=r.Title.Trim(), Description=r.Description, CategoryId=r.CategoryId, CountryCode=r.CountryCode, Brand=r.Brand, LicenseType=r.LicenseType, LicenseReference=r.LicenseReference, ThumbnailUrl=r.ThumbnailUrl, AssetUrl=r.AssetUrl, Difficulty=r.Difficulty, RegionCount=r.RegionCount, IsPublished=r.IsPublished, PublishedAtUtc=r.IsPublished?DateTime.UtcNow:null, CreatedByUserId=userId };
        await artworks.AddAsync(e, ct); audit.Add(Audit(userId,"Artwork",e.Id,"Create",r)); await uow.SaveChangesAsync(ct); return Map(e);
    }

    public async Task<ArtworkDto?> UpdateArtworkAsync(Guid userId, Guid id, UpdateArtworkRequest r, CancellationToken ct)
    {
        ValidateArtwork(r.Title, r.Difficulty, r.RegionCount); var e = await artworks.GetAsync(id, ct); if (e is null) return null;
        e.Title=r.Title.Trim(); e.Description=r.Description; e.CategoryId=r.CategoryId; e.CountryCode=r.CountryCode; e.Brand=r.Brand; e.LicenseType=r.LicenseType; e.LicenseReference=r.LicenseReference; e.ThumbnailUrl=r.ThumbnailUrl; e.AssetUrl=r.AssetUrl; e.Difficulty=r.Difficulty; e.RegionCount=r.RegionCount; e.IsPublished=r.IsPublished; e.PublishedAtUtc ??= r.IsPublished ? DateTime.UtcNow : null; e.UpdatedAtUtc=DateTime.UtcNow; e.UpdatedByUserId=userId;
        audit.Add(Audit(userId,"Artwork",e.Id,"Update",r)); await uow.SaveChangesAsync(ct); return Map(e);
    }

    public Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken ct) => users.ListAsync(ct);
    public Task<IReadOnlyCollection<AuditLogDto>> GetAuditAsync(CancellationToken ct) => audit.ListAsync(500, ct);
    public async Task<AdminMetricsResponse> GetMetricsAsync(CancellationToken ct) => new((await users.ListAsync(ct)).Count, await artworks.CountPublishedAsync(ct), await progress.CountCompletedAsync(ct), await activities.CountAsync(ct));

    private static void ValidateArtwork(string title,int difficulty,int regions){ if(string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required."); if(difficulty is <1 or >5) throw new ArgumentException("Difficulty must be between 1 and 5."); if(regions<0) throw new ArgumentException("RegionCount cannot be negative."); }
    private static AuditLog Audit(Guid userId,string entity,Guid id,string action,object data)=>new(){UserId=userId,EntityName=entity,EntityId=id.ToString(),Action=action,ChangesJson=JsonSerializer.Serialize(data)};
    private static ArtworkDto Map(Artwork x)=>new(x.Id,x.Title,x.Description,x.CategoryId,x.CountryCode,x.Brand,x.LicenseType,x.LicenseReference,x.ThumbnailUrl,x.AssetUrl,x.Difficulty,x.RegionCount,x.IsPublished,x.PublishedAtUtc);
}
