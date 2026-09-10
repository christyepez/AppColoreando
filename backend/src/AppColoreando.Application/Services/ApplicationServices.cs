using System.Text.Json;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Services;

public sealed class AuthService(
    IUserRepository users,
    IUserProfileRepository profiles,
    IRefreshTokenRepository refreshTokens,
    IUserActivityRepository activities,
    IUserMetricRepository metrics,
    IPasswordService passwords,
    ITokenService tokens,
    IUnitOfWork uow) : IAuthService
{
    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (request.Password.Length < 10) throw new ArgumentException("Password must contain at least 10 characters.");
        if (string.IsNullOrWhiteSpace(request.DisplayName)) throw new ArgumentException("DisplayName is required.");
        if (await users.ExistsByEmailAsync(email, ct)) throw new InvalidOperationException("Email already registered.");

        var user = new AppUser { Email = email, DisplayName = request.DisplayName.Trim() };
        user.PasswordHash = passwords.Hash(user, request.Password);
        await users.AddAsync(user, ct);
        profiles.Add(new UserProfile { UserId = user.Id, Locale = string.IsNullOrWhiteSpace(request.Locale) ? "es" : request.Locale!.Trim() });
        await uow.SaveChangesAsync(ct);
        return ToUser(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email), ct);
        if (user is null || !user.IsActive || !passwords.Verify(user, user.PasswordHash, request.Password)) return null;
        user.LastLoginAtUtc = DateTime.UtcNow;
        activities.Add(new UserActivityHistory { UserId = user.Id, ActivityType = "Login" });
        var metric = await GetOrCreateMetric(metrics, user.Id, ct);
        metric.Sessions++;
        var response = IssueTokens(user, request.DeviceId, request.DeviceName, ipAddress);
        await uow.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var tokenHash = tokens.HashRefreshToken(request.RefreshToken);
        var existing = await refreshTokens.GetByHashAsync(tokenHash, ct);
        if (existing is null) return null;

        if (!existing.IsActive)
        {
            if (!string.IsNullOrWhiteSpace(existing.ReplacedByTokenHash))
            {
                await RevokeActiveSessionsAsync(existing.UserId, "RefreshTokenReplayDetected", ct);
            }
            return null;
        }

        if (!StringComparer.Ordinal.Equals(existing.DeviceId, request.DeviceId))
        {
            activities.Add(new UserActivityHistory { UserId = existing.UserId, ActivityType = "RefreshTokenDeviceMismatch" });
            await uow.SaveChangesAsync(ct);
            return null;
        }

        var user = await users.GetByIdAsync(existing.UserId, ct);
        if (user is null || !user.IsActive) return null;
        existing.RevokedAtUtc = DateTime.UtcNow;
        var response = IssueTokens(user, existing.DeviceId, existing.DeviceName, existing.IpAddress);
        existing.ReplacedByTokenHash = tokens.HashRefreshToken(response.RefreshToken);
        activities.Add(new UserActivityHistory { UserId = user.Id, ActivityType = "TokenRefreshed" });
        await uow.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(RevokeTokenRequest request, CancellationToken ct)
    {
        var existing = await refreshTokens.GetByHashAsync(tokens.HashRefreshToken(request.RefreshToken), ct);
        if (existing is not null && existing.RevokedAtUtc is null) existing.RevokedAtUtc = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
    }

    public Task RevokeAllAsync(Guid userId, CancellationToken ct) => RevokeActiveSessionsAsync(userId, "SessionsRevoked", ct);

    private async Task RevokeActiveSessionsAsync(Guid userId, string activityType, CancellationToken ct)
    {
        foreach (var token in await refreshTokens.ListActiveAsync(userId, ct))
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }
        activities.Add(new UserActivityHistory { UserId = userId, ActivityType = activityType });
        await uow.SaveChangesAsync(ct);
    }

    private AuthResponse IssueTokens(AppUser user, string deviceId, string deviceName, string? ipAddress)
    {
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();
        refreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokens.HashRefreshToken(refresh),
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? "unknown" : deviceId.Trim(),
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Unknown" : deviceName.Trim(),
            IpAddress = ipAddress,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        });
        return new AuthResponse(access.Token, refresh, access.ExpiresAtUtc, ToUser(user));
    }

    internal static UserDto ToUser(AppUser x) => new(x.Id, x.Email, x.DisplayName, x.Role, x.IsActive, x.CreatedAtUtc, x.LastLoginAtUtc);

    internal static async Task<UserMetricDaily> GetOrCreateMetric(IUserMetricRepository repo, Guid userId, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var metric = await repo.GetAsync(userId, date, ct);
        if (metric is not null) return metric;
        metric = new UserMetricDaily { UserId = userId, MetricDate = date };
        repo.Add(metric);
        return metric;
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@')) throw new ArgumentException("Invalid email.");
        return normalized;
    }
}

public sealed class UserAccountService(IUserRepository users, IUserProfileRepository profiles, IRefreshTokenRepository tokens, IUnitOfWork uow) : IUserAccountService
{
    public async Task<(UserDto User, UserProfileDto Profile)> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new KeyNotFoundException("User not found.");
        var profile = await GetOrCreateProfile(userId, ct);
        return (AuthService.ToUser(user), MapProfile(profile));
    }

    public async Task<(UserDto User, UserProfileDto Profile)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new KeyNotFoundException("User not found.");
        if (string.IsNullOrWhiteSpace(request.DisplayName)) throw new ArgumentException("DisplayName is required.");
        user.DisplayName = request.DisplayName.Trim();
        var profile = await GetOrCreateProfile(userId, ct);
        profile.Locale = NormalizeOptional(request.Locale, "es");
        profile.KidsModeEnabled = request.KidsModeEnabled;
        profile.SoundEnabled = request.SoundEnabled;
        profile.HapticsEnabled = request.HapticsEnabled;
        profile.DifficultyPreference = NormalizeOptional(request.DifficultyPreference, "Any");
        profile.ThemePreference = NormalizeOptional(request.ThemePreference, "System");
        await uow.SaveChangesAsync(ct);
        return (AuthService.ToUser(user), MapProfile(profile));
    }

    public Task<IReadOnlyCollection<UserSessionDto>> GetSessionsAsync(Guid userId, CancellationToken ct) => tokens.ListSessionsAsync(userId, ct);

    private async Task<UserProfile> GetOrCreateProfile(Guid userId, CancellationToken ct)
    {
        var profile = await profiles.GetAsync(userId, ct);
        if (profile is not null) return profile;
        profile = new UserProfile { UserId = userId };
        profiles.Add(profile);
        return profile;
    }

    private static UserProfileDto MapProfile(UserProfile x) => new(x.Locale, x.KidsModeEnabled, x.SoundEnabled, x.HapticsEnabled, x.DifficultyPreference, x.ThemePreference);
    private static string NormalizeOptional(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

public sealed class CatalogService(IArtworkRepository artworks, ICategoryRepository categories, ICountryRepository countries, ICollectionRepository collections, IGenerationPublicationStore publication) : ICatalogService
{
    public async Task<ArtworkDto?> GetArtworkAsync(Guid id, CancellationToken ct)
    {
        var artwork = await artworks.GetAsync(id, ct);
        return artwork is null || !artwork.IsPublished ? null : ArtworkRepositoryMap(artwork);
    }

    private static ArtworkDto ArtworkRepositoryMap(Artwork x) => new(x.Id, x.Title, x.Description, x.CategoryId, x.CountryCode, x.BrandId, x.LicenseAgreementId, x.LicenseType, x.LicenseReference, x.ThumbnailUrl, x.AssetUrl, x.BundleChecksum, x.Difficulty, x.RegionCount, x.PublishingStatus, x.ScheduledPublishAtUtc, x.PublishedAtUtc, x.Assets.Select(a => new ArtworkAssetDto(a.Id, a.Kind, a.Uri, a.ContentType, a.Checksum, a.SizeBytes, a.IsPrimary)).ToArray());

    public async Task<GenerationArtifactDto?> GetArtworkArtifactAsync(Guid id, string kind, CancellationToken ct)
    {
        if (!await artworks.PublishedExistsAsync(id, ct)) return null;
        return await publication.ReadPublishedAsync(id, kind, ct);
    }

    public Task<PageResult<ArtworkDto>> SearchAsync(CatalogQuery query, CancellationToken ct) => artworks.SearchAsync(query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) }, ct);
    public Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(CancellationToken ct) => categories.ListAsync(ct);
    public Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(CancellationToken ct) => countries.ListAsync(ct);
    public Task<IReadOnlyCollection<CollectionDto>> GetCollectionsAsync(CancellationToken ct) => collections.ListAsync(ct);
}

public sealed class UserContentService(
    IArtworkRepository artworks,
    IUserProgressRepository progress,
    ISyncOperationRepository syncOps,
    IUserActivityRepository activities,
    IUserMetricRepository metrics,
    IUserAchievementRepository achievements,
    IUnitOfWork uow) : IUserContentService
{
    public async Task<IReadOnlyCollection<ProgressDto>> GetProgressAsync(Guid userId, CancellationToken ct) => (await progress.ListAsync(userId, ct)).Select(Map).ToArray();

    public async Task<SyncProgressResponse> SaveProgressAsync(Guid userId, Guid artworkId, SaveProgressRequest request, CancellationToken ct)
    {
        if (!await artworks.PublishedExistsAsync(artworkId, ct)) throw new KeyNotFoundException("Artwork not found.");
        if (string.IsNullOrWhiteSpace(request.ClientOperationId)) throw new ArgumentException("ClientOperationId is required.");
        if (await syncOps.ExistsAsync(userId, request.ClientOperationId, ct))
        {
            var current = await progress.GetAsync(userId, artworkId, ct) ?? throw new KeyNotFoundException("Progress not found.");
            return new SyncProgressResponse(Map(current), SyncOperationStatus.Applied, null);
        }

        var item = await progress.GetAsync(userId, artworkId, ct);
        if (item is null)
        {
            item = new UserArtworkProgress { UserId = userId, ArtworkId = artworkId };
            progress.Add(item);
        }
        else if (request.ClientRevision != item.Revision)
        {
            syncOps.Add(new SyncOperation { UserId = userId, ArtworkId = artworkId, ClientOperationId = request.ClientOperationId, ClientRevision = request.ClientRevision, ServerRevision = item.Revision, Status = SyncOperationStatus.Conflict, ConflictReason = "Server revision differs from client revision." });
            await uow.SaveChangesAsync(ct);
            return new SyncProgressResponse(Map(item), SyncOperationStatus.Conflict, "Server revision differs from client revision.");
        }

        var distinctRegions = request.CompletedRegionIds.Where(x => x >= 0).Distinct().Order().ToArray();
        item.CompletionPercent = Math.Clamp(request.CompletionPercent, 0, 100);
        item.CompletedRegionIdsJson = JsonSerializer.Serialize(distinctRegions);
        item.LastOpenedAtUtc = DateTime.UtcNow;
        item.IsFavorite = request.IsFavorite;
        item.IsDownloaded = request.IsDownloaded;
        item.Revision++;
        var completedNow = item.CompletionPercent >= 100 && item.CompletedAtUtc is null;
        if (completedNow) item.CompletedAtUtc = DateTime.UtcNow;
        activities.Add(new UserActivityHistory { UserId = userId, ArtworkId = artworkId, ActivityType = completedNow ? "ArtworkCompleted" : "ProgressSaved", MetadataJson = JsonSerializer.Serialize(new { item.CompletionPercent, regionCount = distinctRegions.Length, item.Revision }) });
        syncOps.Add(new SyncOperation { UserId = userId, ArtworkId = artworkId, ClientOperationId = request.ClientOperationId, ClientRevision = request.ClientRevision, ServerRevision = item.Revision, Status = SyncOperationStatus.Applied });
        var metric = await AuthService.GetOrCreateMetric(metrics, userId, ct);
        metric.ArtworksOpened++;
        metric.RegionsColored += distinctRegions.Length;
        if (completedNow) metric.ArtworksCompleted++;
        await AwardAchievements(userId, completedNow, distinctRegions.Length, ct);
        await uow.SaveChangesAsync(ct);
        return new SyncProgressResponse(Map(item), SyncOperationStatus.Applied, null);
    }

    public async Task<UserLibraryResponse> GetLibraryAsync(Guid userId, CancellationToken ct)
    {
        var all = (await progress.ListAsync(userId, ct)).Select(Map).ToArray();
        return new UserLibraryResponse(
            all.Where(x => x.IsFavorite).ToArray(),
            all.Where(x => x.CompletionPercent > 0 && x.CompletedAtUtc is null).ToArray(),
            all.Where(x => x.CompletedAtUtc is not null).ToArray(),
            all.OrderByDescending(x => x.LastOpenedAtUtc).Take(20).ToArray(),
            all.Where(x => x.IsDownloaded).ToArray());
    }

    public Task<IReadOnlyCollection<UserActivityDto>> GetHistoryAsync(Guid userId, CancellationToken ct) => activities.ListAsync(userId, 250, ct);

    public async Task<UserMetricsResponse> GetMetricsAsync(Guid userId, CancellationToken ct)
    {
        var data = await metrics.ListAsync(userId, 90, ct);
        var daily = data.Select(x => new DailyMetricDto(x.MetricDate, x.Sessions, x.ArtworksOpened, x.ArtworksCompleted, x.RegionsColored, x.ActiveSeconds)).ToArray();
        return new UserMetricsResponse(daily, data.Sum(x => x.Sessions), data.Sum(x => x.ArtworksOpened), data.Sum(x => x.ArtworksCompleted), data.Sum(x => x.RegionsColored), data.Sum(x => x.ActiveSeconds));
    }

    public async Task<UserAchievementSummary> GetAchievementsAsync(Guid userId, CancellationToken ct)
    {
        var data = await achievements.ListAsync(userId, ct);
        var streak = (await metrics.ListAsync(userId, 30, ct)).OrderByDescending(x => x.MetricDate).TakeWhile(x => x.ArtworksOpened > 0 || x.RegionsColored > 0).Count();
        return new UserAchievementSummary(data.Sum(x => x.XpAwarded), streak, data.Select(x => new AchievementDto(x.Code, x.Name, x.XpAwarded, x.EarnedAtUtc)).ToArray());
    }

    private async Task AwardAchievements(Guid userId, bool completedNow, int regions, CancellationToken ct)
    {
        foreach (var rule in GamificationRules.Evaluate(completedNow, regions))
        {
            if (!await achievements.ExistsAsync(userId, rule.Code, ct))
            {
                achievements.Add(new UserAchievement { UserId = userId, Code = rule.Code, Name = rule.Name, XpAwarded = rule.XpAwarded });
            }
        }
    }

    private static ProgressDto Map(UserArtworkProgress x)
    {
        var ids = JsonSerializer.Deserialize<int[]>(x.CompletedRegionIdsJson) ?? [];
        return new ProgressDto(x.ArtworkId, x.CompletionPercent, ids, x.IsFavorite, x.IsDownloaded, x.Revision, x.LastOpenedAtUtc, x.CompletedAtUtc);
    }
}

public sealed class AdminService(
    ICategoryRepository categories,
    ICountryRepository countries,
    IBrandRepository brands,
    ILicenseRepository licenses,
    ICollectionRepository collections,
    IArtworkRepository artworks,
    IUserRepository users,
    IUserProgressRepository progress,
    IUserActivityRepository activities,
    IAuditRepository audit,
    IUnitOfWork uow) : IAdminService
{
    public async Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Slug)) throw new ArgumentException("Name and Slug are required.");
        var entity = new Category { Name = r.Name.Trim(), Slug = Slug(r.Slug), IsActive = r.IsActive, CreatedByUserId = userId };
        await categories.AddAsync(entity, ct);
        audit.Add(Audit(userId, "Category", entity.Id, "Create", r));
        await uow.SaveChangesAsync(ct);
        return new(entity.Id, entity.Name, entity.Slug, entity.IsActive);
    }

    public async Task<CountryDto> UpsertCountryAsync(Guid userId, UpsertCountryRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Code) || string.IsNullOrWhiteSpace(r.Name)) throw new ArgumentException("Code and Name are required.");
        var code = r.Code.Trim().ToUpperInvariant();
        var e = await countries.GetByCodeAsync(code, ct);
        if (e is null) { e = new Country { Code = code, CreatedByUserId = userId }; countries.Add(e); }
        e.Name = r.Name.Trim(); e.IsActive = r.IsActive; e.UpdatedAtUtc = DateTime.UtcNow; e.UpdatedByUserId = userId;
        audit.Add(Audit(userId, "Country", e.Id, "Upsert", r));
        await uow.SaveChangesAsync(ct);
        return new(e.Id, e.Code, e.Name, e.IsActive);
    }

    public async Task<BrandDto> UpsertBrandAsync(Guid userId, Guid? id, UpsertBrandRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Slug)) throw new ArgumentException("Name and Slug are required.");
        var e = id.HasValue ? await brands.GetAsync(id.Value, ct) : null;
        if (e is null) { e = new Brand { CreatedByUserId = userId }; brands.Add(e); }
        e.Name = r.Name.Trim(); e.Slug = Slug(r.Slug); e.Enabled = r.Enabled; e.Notes = r.Notes; e.UpdatedAtUtc = DateTime.UtcNow; e.UpdatedByUserId = userId;
        audit.Add(Audit(userId, "Brand", e.Id, "Upsert", r));
        await uow.SaveChangesAsync(ct);
        return new(e.Id, e.Name, e.Slug, e.Enabled, e.Notes);
    }

    public async Task<LicenseAgreementDto> UpsertLicenseAsync(Guid userId, Guid? id, UpsertLicenseAgreementRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.RightsSummary)) throw new ArgumentException("Name and RightsSummary are required.");
        var e = id.HasValue ? await licenses.GetAsync(id.Value, ct) : null;
        if (e is null) { e = new LicenseAgreement { CreatedByUserId = userId }; licenses.Add(e); }
        e.BrandId = r.BrandId; e.Name = r.Name.Trim(); e.RightsSummary = r.RightsSummary.Trim(); e.ValidFromUtc = r.ValidFromUtc; e.ValidToUtc = r.ValidToUtc; e.AllowsStoreDistribution = r.AllowsStoreDistribution; e.AllowsKidsMode = r.AllowsKidsMode; e.AllowsOfflineDownload = r.AllowsOfflineDownload; e.FeatureEnabled = r.FeatureEnabled;
        e.Territories.Clear();
        foreach (var t in r.Territories) e.Territories.Add(new LicenseTerritory { LicenseAgreementId = e.Id, CountryCode = t.CountryCode.Trim().ToUpperInvariant(), IsAllowed = t.IsAllowed });
        audit.Add(Audit(userId, "LicenseAgreement", e.Id, "Upsert", r));
        await uow.SaveChangesAsync(ct);
        return MapLicense(e);
    }

    public async Task<CollectionDto> UpsertCollectionAsync(Guid userId, Guid? id, UpsertCollectionRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Slug)) throw new ArgumentException("Name and Slug are required.");
        var e = id.HasValue ? await collections.GetAsync(id.Value, ct) : null;
        if (e is null) { e = new Collection { CreatedByUserId = userId }; collections.Add(e); }
        e.Name = r.Name.Trim(); e.Slug = Slug(r.Slug); e.Description = r.Description; e.CountryCode = r.CountryCode?.Trim().ToUpperInvariant(); e.IsActive = r.IsActive; e.UpdatedAtUtc = DateTime.UtcNow; e.UpdatedByUserId = userId;
        e.Artworks.Clear();
        var order = 0;
        foreach (var artworkId in r.ArtworkIds.Distinct()) e.Artworks.Add(new CollectionArtwork { CollectionId = e.Id, ArtworkId = artworkId, SortOrder = order++ });
        audit.Add(Audit(userId, "Collection", e.Id, "Upsert", r));
        await uow.SaveChangesAsync(ct);
        return new(e.Id, e.Name, e.Slug, e.Description, e.CountryCode, e.IsActive, e.Artworks.Count);
    }

    public async Task<ArtworkDto> CreateArtworkAsync(Guid userId, UpsertArtworkRequest r, CancellationToken ct)
    {
        ValidateArtwork(r);
        var e = new Artwork { CreatedByUserId = userId };
        ApplyArtwork(e, r, userId);
        await artworks.AddAsync(e, ct);
        audit.Add(Audit(userId, "Artwork", e.Id, "Create", r));
        await uow.SaveChangesAsync(ct);
        return MapArtwork(e);
    }

    public async Task<ArtworkDto?> UpdateArtworkAsync(Guid userId, Guid id, UpsertArtworkRequest r, CancellationToken ct)
    {
        ValidateArtwork(r);
        var e = await artworks.GetAsync(id, ct);
        if (e is null) return null;
        ApplyArtwork(e, r, userId);
        audit.Add(Audit(userId, "Artwork", e.Id, "Update", r));
        await uow.SaveChangesAsync(ct);
        return MapArtwork(e);
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, Guid targetUserId, AdminUpdateUserRequest request, CancellationToken ct)
    {
        var target = await users.GetByIdAsync(targetUserId, ct);
        if (target is null) return null;
        if (!Enum.TryParse<AppRole>(request.Role, true, out var role)) throw new ArgumentException("Invalid role.");
        target.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? target.DisplayName : request.DisplayName.Trim();
        target.Role = role.ToString();
        target.IsActive = request.IsActive;
        target.UpdatedAtUtc = DateTime.UtcNow;
        target.UpdatedByUserId = userId;
        audit.Add(Audit(userId, "AppUser", target.Id, "Update", request));
        await uow.SaveChangesAsync(ct);
        return AuthService.ToUser(target);
    }

    public Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken ct) => users.ListAsync(ct);
    public Task<IReadOnlyCollection<BrandDto>> GetBrandsAsync(CancellationToken ct) => brands.ListAsync(ct);
    public Task<IReadOnlyCollection<LicenseAgreementDto>> GetLicensesAsync(CancellationToken ct) => licenses.ListAsync(ct);
    public Task<IReadOnlyCollection<AuditLogDto>> GetAuditAsync(CancellationToken ct) => audit.ListAsync(500, ct);
    public async Task<AdminMetricsResponse> GetMetricsAsync(CancellationToken ct) => new((await users.ListAsync(ct)).Count, await artworks.CountPublishedAsync(ct), await progress.CountCompletedAsync(ct), await activities.CountAsync(ct));

    private static void ApplyArtwork(Artwork e, UpsertArtworkRequest r, Guid userId)
    {
        e.Title = r.Title.Trim();
        e.Description = r.Description;
        e.CategoryId = r.CategoryId;
        e.CountryCode = r.CountryCode?.Trim().ToUpperInvariant();
        e.BrandId = r.BrandId;
        e.LicenseAgreementId = r.LicenseAgreementId;
        e.LicenseType = r.LicenseType.Trim();
        e.LicenseReference = r.LicenseReference;
        e.ThumbnailUrl = r.ThumbnailUrl;
        e.AssetUrl = r.AssetUrl;
        e.BundleChecksum = r.BundleChecksum;
        e.Difficulty = r.Difficulty;
        e.RegionCount = r.RegionCount;
        e.PublishingStatus = r.PublishingStatus;
        e.ScheduledPublishAtUtc = r.ScheduledPublishAtUtc;
        e.PublishedAtUtc = r.PublishingStatus == PublishingStatus.Published ? e.PublishedAtUtc ?? DateTime.UtcNow : null;
        e.UpdatedAtUtc = DateTime.UtcNow;
        e.UpdatedByUserId = userId;
        e.Assets.Clear();
        foreach (var asset in r.Assets) e.Assets.Add(new ArtworkAsset { ArtworkId = e.Id, Kind = asset.Kind, Uri = asset.Uri, ContentType = asset.ContentType, Checksum = asset.Checksum, SizeBytes = asset.SizeBytes, IsPrimary = asset.IsPrimary });
    }

    private static void ValidateArtwork(UpsertArtworkRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Title)) throw new ArgumentException("Title is required.");
        if (r.Difficulty is < 1 or > 5) throw new ArgumentException("Difficulty must be between 1 and 5.");
        if (r.RegionCount < 0) throw new ArgumentException("RegionCount cannot be negative.");
        if (r.PublishingStatus == PublishingStatus.Scheduled && r.ScheduledPublishAtUtc is null) throw new ArgumentException("Scheduled artwork requires ScheduledPublishAtUtc.");
    }

    private static string Slug(string value) => value.Trim().ToLowerInvariant().Replace(' ', '-');
    private static AuditLog Audit(Guid userId, string entity, Guid id, string action, object data) => new() { UserId = userId, EntityName = entity, EntityId = id.ToString(), Action = action, ChangesJson = JsonSerializer.Serialize(data) };
    private static ArtworkDto MapArtwork(Artwork x) => new(x.Id, x.Title, x.Description, x.CategoryId, x.CountryCode, x.BrandId, x.LicenseAgreementId, x.LicenseType, x.LicenseReference, x.ThumbnailUrl, x.AssetUrl, x.BundleChecksum, x.Difficulty, x.RegionCount, x.PublishingStatus, x.ScheduledPublishAtUtc, x.PublishedAtUtc, x.Assets.Select(a => new ArtworkAssetDto(a.Id, a.Kind, a.Uri, a.ContentType, a.Checksum, a.SizeBytes, a.IsPrimary)).ToArray());
    private static LicenseAgreementDto MapLicense(LicenseAgreement x) => new(x.Id, x.BrandId, x.Name, x.RightsSummary, x.ValidFromUtc, x.ValidToUtc, x.AllowsStoreDistribution, x.AllowsKidsMode, x.AllowsOfflineDownload, x.FeatureEnabled, x.Territories.Select(t => new LicenseTerritoryDto(t.Id, t.CountryCode, t.IsAllowed)).ToArray());
}

public sealed class ArtworkBundleProcessor : IArtworkBundleProcessor
{
    public BundleValidationResult Validate(string bundleJson)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(bundleJson))
        {
            return Invalid("Bundle JSON is required.");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(bundleJson);
        }
        catch (JsonException)
        {
            return Invalid("Bundle JSON is invalid.");
        }

        using (doc)
        {
            var root = doc.RootElement;
            var paletteCount = root.TryGetProperty("palette", out var palette) && palette.ValueKind == JsonValueKind.Array ? palette.GetArrayLength() : 0;
            var regionCount = root.TryGetProperty("regions", out var regions) && regions.ValueKind == JsonValueKind.Array ? regions.GetArrayLength() : 0;
            if (paletteCount == 0) errors.Add("Palette is required.");
            if (regionCount == 0) errors.Add("At least one region is required.");

            var paletteIds = new HashSet<int>();
            if (paletteCount > 0)
            {
                foreach (var item in palette.EnumerateArray())
                {
                    if (!item.TryGetProperty("id", out var idNode) || !idNode.TryGetInt32(out var id) || id <= 0 || !paletteIds.Add(id))
                        errors.Add("Palette ids must be positive and unique.");
                }
            }

            var regionIds = new HashSet<int>();
            if (regionCount > 0)
            {
                foreach (var region in regions.EnumerateArray())
                {
                    if (!region.TryGetProperty("id", out var idNode) || !idNode.TryGetInt32(out var regionId) || regionId <= 0 || !regionIds.Add(regionId))
                        errors.Add("Region ids must be positive and unique.");
                    if (!region.TryGetProperty("colorId", out var colorNode) || !colorNode.TryGetInt32(out var colorId) || !paletteIds.Contains(colorId))
                        errors.Add("Every region colorId must reference the palette.");
                    if (!region.TryGetProperty("polygon", out var polygon) || polygon.ValueKind != JsonValueKind.Array || polygon.GetArrayLength() < 3)
                        errors.Add("Every region requires a polygon with at least three points.");
                }
            }

            var difficulty = Math.Clamp((regionCount / 20) + (paletteCount / 6) + 1, 1, 5);
            var checksum = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(bundleJson))).ToLowerInvariant();
            return new BundleValidationResult(errors.Count == 0, regionCount, paletteCount, difficulty, checksum, errors.Distinct().ToArray());
        }
    }

    private static BundleValidationResult Invalid(string error) => new(false, 0, 0, 1, string.Empty, [error]);
}

