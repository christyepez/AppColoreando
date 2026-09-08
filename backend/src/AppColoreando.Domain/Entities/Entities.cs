namespace AppColoreando.Domain.Entities;

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
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Artwork : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public string? CountryCode { get; set; }
    public string? Brand { get; set; }
    public string LicenseType { get; set; } = "Original";
    public string? LicenseReference { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AssetUrl { get; set; }
    public int Difficulty { get; set; } = 1;
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
    public bool IsFavorite { get; set; }
    public DateTime LastOpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
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
