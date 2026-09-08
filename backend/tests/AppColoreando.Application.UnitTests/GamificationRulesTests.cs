using AppColoreando.Application.Services;

namespace AppColoreando.Application.UnitTests;

public sealed class GamificationRulesTests
{
    [Fact]
    public void Evaluate_awards_first_color_when_any_region_is_colored()
    {
        var result = GamificationRules.Evaluate(false, 1);
        Assert.Contains(result, x => x.Code == "first-color" && x.XpAwarded == 25);
        Assert.DoesNotContain(result, x => x.Code == "first-artwork");
    }

    [Fact]
    public void Evaluate_awards_completion_and_explorer_rules()
    {
        var result = GamificationRules.Evaluate(true, 50);
        Assert.Contains(result, x => x.Code == "first-artwork" && x.XpAwarded == 100);
        Assert.Contains(result, x => x.Code == "fifty-regions" && x.XpAwarded == 75);
    }
}
