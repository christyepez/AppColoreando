using AppColoreando.Application.Services;

namespace AppColoreando.Application.UnitTests;

public sealed class MonetizationRulesTests
{
    [Fact]
    public void Free_plan_has_expected_limits()
    {
        var entitlements = MonetizationRules.ForPlan(MonetizationRules.FreePlan);
        Assert.False(entitlements.IsPremium);
        Assert.True(entitlements.AdsEnabled);
        Assert.Equal(8, entitlements.MaxOfflineArtworks);
        Assert.False(entitlements.PremiumStylesEnabled);
    }

    [Fact]
    public void Premium_plan_unlocks_features()
    {
        var entitlements = MonetizationRules.ForPlan(MonetizationRules.PremiumPlan);
        Assert.True(entitlements.IsPremium);
        Assert.False(entitlements.AdsEnabled);
        Assert.True(entitlements.PremiumStylesEnabled);
        Assert.True(entitlements.EventBoostsEnabled);
    }
}