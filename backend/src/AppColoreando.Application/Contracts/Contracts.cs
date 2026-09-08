namespace AppColoreando.Application.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record UserDto(Guid Id, string Email, string DisplayName, string Role, bool IsActive, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc);
public sealed record AuthResponse(string AccessToken, UserDto User);
public sealed record CategoryDto(Guid Id, string Name, string Slug, bool IsActive);
public sealed record CreateCategoryRequest(string Name, string Slug, bool IsActive = true);
public sealed record ArtworkDto(Guid Id, string Title, string? Description, Guid CategoryId, string? CountryCode, string? Brand, string LicenseType, string? LicenseReference, string? ThumbnailUrl, string? AssetUrl, int Difficulty, int RegionCount, bool IsPublished, DateTime? PublishedAtUtc);
public sealed record CreateArtworkRequest(string Title, string? Description, Guid CategoryId, string? CountryCode, string? Brand, string LicenseType, string? LicenseReference, string? ThumbnailUrl, string? AssetUrl, int Difficulty, int RegionCount, bool IsPublished);
public sealed record UpdateArtworkRequest(string Title, string? Description, Guid CategoryId, string? CountryCode, string? Brand, string LicenseType, string? LicenseReference, string? ThumbnailUrl, string? AssetUrl, int Difficulty, int RegionCount, bool IsPublished);
public sealed record SaveProgressRequest(decimal CompletionPercent, IReadOnlyCollection<int> CompletedRegionIds, bool IsFavorite);
public sealed record ProgressDto(Guid ArtworkId, decimal CompletionPercent, IReadOnlyCollection<int> CompletedRegionIds, bool IsFavorite, DateTime LastOpenedAtUtc, DateTime? CompletedAtUtc);
public sealed record UserActivityDto(Guid Id, Guid? ArtworkId, string ActivityType, string? MetadataJson, DateTime OccurredAtUtc);
public sealed record DailyMetricDto(DateOnly Date, int Sessions, int ArtworksOpened, int ArtworksCompleted, int RegionsColored, int ActiveSeconds);
public sealed record UserMetricsResponse(IReadOnlyCollection<DailyMetricDto> Daily, int Sessions, int ArtworksOpened, int ArtworksCompleted, int RegionsColored, int ActiveSeconds);
public sealed record AdminMetricsResponse(int Users, int PublishedArtworks, int Completions, int ActivityEvents);
public sealed record AuditLogDto(Guid Id, Guid? UserId, string EntityName, string EntityId, string Action, string? ChangesJson, DateTime OccurredAtUtc);
