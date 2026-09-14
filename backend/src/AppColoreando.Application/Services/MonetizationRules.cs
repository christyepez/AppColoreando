using AppColoreando.Application.Contracts;

namespace AppColoreando.Application.Services;

public static class MonetizationRules
{
    public const string FreePlan = "free";
    public const string PremiumPlan = "premium";

    public static MonetizationEntitlementsResponse ForPlan(string? planCode)
    {
        var premium = string.Equals(planCode, PremiumPlan, StringComparison.OrdinalIgnoreCase);
        return premium
            ? new MonetizationEntitlementsResponse(PremiumPlan, true, false, 100, true, true, true)
            : new MonetizationEntitlementsResponse(FreePlan, false, true, 8, false, false, false);
    }

    public static IReadOnlyCollection<MonetizationPlanDto> Plans() =>
    [
        new(FreePlan, "Free", true, 8, true),
        new(PremiumPlan, "Premium", true, 100, false)
    ];
}