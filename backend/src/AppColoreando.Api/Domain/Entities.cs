namespace AppColoreando.Api.Domain;

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
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class Category : AuditableEntity
{
    public required string Name { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Artwork : AuditableEntity
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string LicenseType { get; set; } = "Original";
    public string LicenseReference { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string AssetUrl { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Easy";
    public int RegionCount { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}

public sealed class UserArtworkProgress : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ArtworkId { get; set; }
    public decimal CompletionPercent { get; set; }
    public string CompletedRegionIdsJson { get; set; } = "[]";
    public DateTime LastOpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsFavorite { get; set; }
}

public sealed class UserActivityHistory : AuditableEntity
{
    public Guid UserId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public Guid? ArtworkId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuditLog
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UserMetricDaily
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly MetricDate { get; set; }
    public int Sessions { get; set; }
    public int ArtworksOpened { get; set; }
    public int ArtworksCompleted { get; set; }
    public int RegionsColored { get; set; }
    public int ActiveSeconds { get; set; }
}
