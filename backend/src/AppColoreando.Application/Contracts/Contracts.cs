using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Contracts;

public sealed record PageResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int Total);
public sealed record CatalogQuery(string? Search, string? CountryCode, Guid? CategoryId, Guid? CollectionId, int? Difficulty, bool? LicensedOnly, int Page = 1, int PageSize = 24);

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string? Locale = "es");
public sealed record LoginRequest(string Email, string Password, string DeviceId = "web", string DeviceName = "Browser");
public sealed record RefreshTokenRequest(string RefreshToken, string DeviceId);
public sealed record RevokeTokenRequest(string RefreshToken);
public sealed record UserDto(Guid Id, string Email, string DisplayName, string Role, bool IsActive, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, UserDto User);
public sealed record UserProfileDto(string Locale, bool KidsModeEnabled, bool SoundEnabled, bool HapticsEnabled, string DifficultyPreference, string ThemePreference);
public sealed record UpdateProfileRequest(string DisplayName, string Locale, bool KidsModeEnabled, bool SoundEnabled, bool HapticsEnabled, string DifficultyPreference, string ThemePreference);
public sealed record UserSessionDto(Guid Id, string DeviceId, string DeviceName, DateTime ExpiresAtUtc, DateTime? RevokedAtUtc, DateTime CreatedAtUtc);

public sealed record CategoryDto(Guid Id, string Name, string Slug, bool IsActive);
public sealed record CreateCategoryRequest(string Name, string Slug, bool IsActive = true);
public sealed record CountryDto(Guid Id, string Code, string Name, bool IsActive);
public sealed record UpsertCountryRequest(string Code, string Name, bool IsActive = true);
public sealed record BrandDto(Guid Id, string Name, string Slug, bool Enabled, string? Notes);
public sealed record UpsertBrandRequest(string Name, string Slug, bool Enabled, string? Notes);
public sealed record LicenseTerritoryDto(Guid Id, string CountryCode, bool IsAllowed);
public sealed record UpsertLicenseTerritoryRequest(string CountryCode, bool IsAllowed);
public sealed record LicenseAgreementDto(Guid Id, Guid? BrandId, string Name, string RightsSummary, DateTime ValidFromUtc, DateTime? ValidToUtc, bool AllowsStoreDistribution, bool AllowsKidsMode, bool AllowsOfflineDownload, bool FeatureEnabled, IReadOnlyCollection<LicenseTerritoryDto> Territories);
public sealed record UpsertLicenseAgreementRequest(Guid? BrandId, string Name, string RightsSummary, DateTime ValidFromUtc, DateTime? ValidToUtc, bool AllowsStoreDistribution, bool AllowsKidsMode, bool AllowsOfflineDownload, bool FeatureEnabled, IReadOnlyCollection<UpsertLicenseTerritoryRequest> Territories);
public sealed record CollectionDto(Guid Id, string Name, string Slug, string? Description, string? CountryCode, bool IsActive, int ArtworkCount);
public sealed record UpsertCollectionRequest(string Name, string Slug, string? Description, string? CountryCode, bool IsActive, IReadOnlyCollection<Guid> ArtworkIds);

public sealed record ArtworkAssetDto(Guid Id, ArtworkAssetKind Kind, string Uri, string ContentType, string? Checksum, long SizeBytes, bool IsPrimary);
public sealed record UpsertArtworkAssetRequest(ArtworkAssetKind Kind, string Uri, string ContentType, string? Checksum, long SizeBytes, bool IsPrimary);
public sealed record ArtworkDto(Guid Id, string Title, string? Description, Guid CategoryId, string? CountryCode, Guid? BrandId, Guid? LicenseAgreementId, string LicenseType, string? LicenseReference, string? ThumbnailUrl, string? AssetUrl, string? BundleChecksum, int Difficulty, int RegionCount, PublishingStatus PublishingStatus, DateTime? ScheduledPublishAtUtc, DateTime? PublishedAtUtc, IReadOnlyCollection<ArtworkAssetDto> Assets);
public sealed record UpsertArtworkRequest(string Title, string? Description, Guid CategoryId, string? CountryCode, Guid? BrandId, Guid? LicenseAgreementId, string LicenseType, string? LicenseReference, string? ThumbnailUrl, string? AssetUrl, string? BundleChecksum, int Difficulty, int RegionCount, PublishingStatus PublishingStatus, DateTime? ScheduledPublishAtUtc, IReadOnlyCollection<UpsertArtworkAssetRequest> Assets);

public sealed record SaveProgressRequest(decimal CompletionPercent, IReadOnlyCollection<int> CompletedRegionIds, bool IsFavorite, bool IsDownloaded, int ClientRevision, string ClientOperationId);
public sealed record ProgressDto(Guid ArtworkId, decimal CompletionPercent, IReadOnlyCollection<int> CompletedRegionIds, bool IsFavorite, bool IsDownloaded, int Revision, DateTime LastOpenedAtUtc, DateTime? CompletedAtUtc);
public sealed record SyncProgressResponse(ProgressDto Progress, SyncOperationStatus Status, string? ConflictReason);
public sealed record UserLibraryResponse(IReadOnlyCollection<ProgressDto> Favorites, IReadOnlyCollection<ProgressDto> InProgress, IReadOnlyCollection<ProgressDto> Completed, IReadOnlyCollection<ProgressDto> Recent, IReadOnlyCollection<ProgressDto> Downloaded);
public sealed record UserActivityDto(Guid Id, Guid? ArtworkId, string ActivityType, string? MetadataJson, DateTime OccurredAtUtc);
public sealed record DailyMetricDto(DateOnly Date, int Sessions, int ArtworksOpened, int ArtworksCompleted, int RegionsColored, int ActiveSeconds);
public sealed record UserMetricsResponse(IReadOnlyCollection<DailyMetricDto> Daily, int Sessions, int ArtworksOpened, int ArtworksCompleted, int RegionsColored, int ActiveSeconds);
public sealed record AchievementDto(string Code, string Name, int XpAwarded, DateTime EarnedAtUtc);
public sealed record UserAchievementSummary(int Xp, int StreakDays, IReadOnlyCollection<AchievementDto> Achievements);

public sealed record AdminUpdateUserRequest(string DisplayName, string Role, bool IsActive);
public sealed record AdminMetricsResponse(int Users, int PublishedArtworks, int Completions, int ActivityEvents);
public sealed record AuditLogDto(Guid Id, Guid? UserId, string EntityName, string EntityId, string Action, string? ChangesJson, DateTime OccurredAtUtc);

public sealed record BundleValidationResult(bool IsValid, int RegionCount, int PaletteCount, int Difficulty, string Checksum, IReadOnlyCollection<string> Errors);
