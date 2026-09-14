namespace AppColoreando.Application.Services;

public static class TelemetryRules
{
    private static readonly HashSet<string> AllowedEvents = new(StringComparer.Ordinal)
    {
        "app_open", "home_view", "catalog_view", "catalog_search",
        "artwork_open", "artwork_favorite", "color_region", "artwork_complete",
        "daily_open", "event_open", "offline_cache_hit", "offline_cache_miss"
    };

    private static readonly HashSet<string> AllowedPropertyKeys = new(StringComparer.Ordinal)
    {
        "source", "difficulty", "country", "collection",
        "resultCount", "cached", "online", "screen"
    };

    public static bool IsAllowedEvent(string eventName) =>
        !string.IsNullOrWhiteSpace(eventName) && AllowedEvents.Contains(eventName.Trim());

    public static IReadOnlyDictionary<string, string> SanitizeProperties(
        IReadOnlyDictionary<string, string>? properties)
    {
        if (properties is null || properties.Count == 0) return new Dictionary<string, string>();
        return properties
            .Where(x => AllowedPropertyKeys.Contains(x.Key))
            .Take(8)
            .ToDictionary(
                x => x.Key,
                x => NormalizeValue(x.Value),
                StringComparer.Ordinal);
    }

    public static DateTime NormalizeOccurredAt(DateTime? occurredAtUtc, DateTime nowUtc)
    {
        var now = nowUtc.ToUniversalTime();
        if (occurredAtUtc is null) return now;
        var candidate = occurredAtUtc.Value.ToUniversalTime();
        if (candidate < now.AddDays(-7) || candidate > now.AddMinutes(5)) return now;
        return candidate;
    }

    private static string NormalizeValue(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        return normalized.Length <= 64 ? normalized : normalized[..64];
    }
}
