namespace AppColoreando.Domain.Entities;

public enum AppRole { Admin, ContentManager, Analyst, User }
public enum PublishingStatus { Draft, Processing, QA, Approved, Scheduled, Published, Archived }
public enum ArtworkAssetKind { BundleJson, Thumbnail, SourceSvg, PreviewImage }
public enum SyncOperationStatus { Applied, Conflict, Rejected }

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public sealed class AppUser : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = AppRole.User.ToString();
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class UserProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Locale { get; set; } = "es";
    public bool KidsModeEnabled { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public bool HapticsEnabled { get; set; } = true;
    public string DifficultyPreference { get; set; } = "Any";
    public string ThemePreference { get; set; } = "System";
}

public sealed class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}

public sealed class Country : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Brand : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? Notes { get; set; }
}

public sealed class LicenseAgreement : AuditableEntity
{
    public Guid? BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RightsSummary { get; set; } = string.Empty;
    public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ValidToUtc { get; set; }
    public bool AllowsStoreDistribution { get; set; }
    public bool AllowsKidsMode { get; set; }
    public bool AllowsOfflineDownload { get; set; }
    public bool FeatureEnabled { get; set; }
    public List<LicenseTerritory> Territories { get; set; } = [];
}

public sealed class LicenseTerritory : AuditableEntity
{
    public Guid LicenseAgreementId { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
}

public sealed class Collection : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CollectionArtwork> Artworks { get; set; } = [];
}

public sealed class CollectionArtwork
{
    public Guid CollectionId { get; set; }
    public Guid ArtworkId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class Artwork : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public string? CountryCode { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? LicenseAgreementId { get; set; }
    public string LicenseType { get; set; } = "Original";
    public string? LicenseReference { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AssetUrl { get; set; }
    public string? BundleChecksum { get; set; }
    public int Difficulty { get; set; } = 1;
    public int RegionCount { get; set; }
    public PublishingStatus PublishingStatus { get; set; } = PublishingStatus.Draft;
    public DateTime? ScheduledPublishAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public List<ArtworkAsset> Assets { get; set; } = [];
    public bool IsPublished => PublishingStatus == PublishingStatus.Published;
}

public sealed class ArtworkAsset : AuditableEntity
{
    public Guid ArtworkId { get; set; }
    public ArtworkAssetKind Kind { get; set; }
    public string Uri { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? Checksum { get; set; }
    public long SizeBytes { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class UserArtworkProgress : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ArtworkId { get; set; }
    public decimal CompletionPercent { get; set; }
    public string CompletedRegionIdsJson { get; set; } = "[]";
    public bool IsFavorite { get; set; }
    public bool IsDownloaded { get; set; }
    public int Revision { get; set; }
    public DateTime LastOpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class SyncOperation : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ArtworkId { get; set; }
    public string ClientOperationId { get; set; } = string.Empty;
    public int ClientRevision { get; set; }
    public int ServerRevision { get; set; }
    public SyncOperationStatus Status { get; set; }
    public string? ConflictReason { get; set; }
}

public sealed class UserAchievement : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int XpAwarded { get; set; }
    public DateTime EarnedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UserActivityHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? ArtworkId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UserMetricDaily
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly MetricDate { get; set; }
    public int Sessions { get; set; }
    public int ArtworksOpened { get; set; }
    public int ArtworksCompleted { get; set; }
    public int RegionsColored { get; set; }
    public int ActiveSeconds { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ChangesJson { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
