namespace AppColoreando.Application.Services;

public sealed record AchievementRule(string Code, string Name, int XpAwarded);

public static class GamificationRules
{
    private static readonly AchievementRule[] Rules =
    [
        new("first-color", "First Color", 25),
        new("fifty-regions", "Color Explorer", 75),
        new("hundred-regions", "Color Adventurer", 125),
        new("five-hundred-regions", "Color Master", 300),
        new("first-artwork", "First Artwork", 100),
        new("five-artworks", "Creative Momentum", 200),
        new("twenty-artworks", "Gallery Builder", 500),
        new("streak-3", "Three Day Streak", 75),
        new("streak-7", "Seven Day Streak", 200),
        new("streak-30", "Thirty Day Streak", 750),
        new("daily-finish", "Daily Artist", 125),
        new("event-finish", "Event Explorer", 150),
    ];

    public static IReadOnlyCollection<AchievementRule> Evaluate(
        int totalCompletedArtworks,
        int totalRegionsColored,
        int streakDays,
        bool completedDaily,
        bool completedEvent)
    {
        var result = new List<AchievementRule>();
        Add("first-color", totalRegionsColored > 0, result);
        Add("fifty-regions", totalRegionsColored >= 50, result);
        Add("hundred-regions", totalRegionsColored >= 100, result);
        Add("five-hundred-regions", totalRegionsColored >= 500, result);
        Add("first-artwork", totalCompletedArtworks >= 1, result);
        Add("five-artworks", totalCompletedArtworks >= 5, result);
        Add("twenty-artworks", totalCompletedArtworks >= 20, result);
        Add("streak-3", streakDays >= 3, result);
        Add("streak-7", streakDays >= 7, result);
        Add("streak-30", streakDays >= 30, result);
        Add("daily-finish", completedDaily, result);
        Add("event-finish", completedEvent, result);
        return result;
    }


    public static (int Level, int CurrentLevelXp, int NextLevelXp) Progression(int xp)
    {
        var safeXp = Math.Max(0, xp);
        var level = 1;
        var threshold = 250;
        var consumed = 0;
        while (safeXp >= consumed + threshold)
        {
            consumed += threshold;
            level++;
            threshold = 250 + ((level - 1) * 150);
        }
        return (level, safeXp - consumed, threshold);
    }
    private static void Add(string code, bool condition, List<AchievementRule> result)
    {
        if (!condition) return;
        var rule = Rules.First(x => x.Code == code);
        result.Add(rule);
    }
}
