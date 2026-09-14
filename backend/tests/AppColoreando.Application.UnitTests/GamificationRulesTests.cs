using AppColoreando.Application.Services;

namespace AppColoreando.Application.UnitTests;

public sealed class GamificationRulesTests
{
    [Fact]
    public void Evaluate_awards_progression_milestones()
    {
        var result = GamificationRules.Evaluate(5, 120, 7, false, false);
        Assert.Contains(result, x => x.Code == "first-color" && x.XpAwarded == 25);
        Assert.Contains(result, x => x.Code == "hundred-regions" && x.XpAwarded == 125);
        Assert.Contains(result, x => x.Code == "five-artworks" && x.XpAwarded == 200);
        Assert.Contains(result, x => x.Code == "streak-7" && x.XpAwarded == 200);
        Assert.DoesNotContain(result, x => x.Code == "twenty-artworks");
    }

    [Fact]
    public void Evaluate_awards_daily_and_event_completion()
    {
        var result = GamificationRules.Evaluate(1, 50, 3, true, true);
        Assert.Contains(result, x => x.Code == "daily-finish" && x.XpAwarded == 125);
        Assert.Contains(result, x => x.Code == "event-finish" && x.XpAwarded == 150);
        Assert.Contains(result, x => x.Code == "first-artwork" && x.XpAwarded == 100);
        Assert.Contains(result, x => x.Code == "streak-3" && x.XpAwarded == 75);
    }
}
