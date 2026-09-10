namespace AppColoreando.Application.Services;

public sealed record AchievementRule(string Code, string Name, int XpAwarded);

public static class GamificationRules
{
    private static readonly AchievementRule FirstColor = new("first-color", "First Color", 25);
    private static readonly AchievementRule FirstArtwork = new("first-artwork", "First Artwork", 100);
    private static readonly AchievementRule FiftyRegions = new("fifty-regions", "Color Explorer", 75);

    public static IReadOnlyCollection<AchievementRule> Evaluate(bool completedNow, int regionsColored)
    {
        var result = new List<AchievementRule>();
        if (regionsColored > 0) result.Add(FirstColor);
        if (regionsColored >= 50) result.Add(FiftyRegions);
        if (completedNow) result.Add(FirstArtwork);
        return result;
    }
}
