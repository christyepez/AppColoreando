using AppColoreando.Domain.Entities;

namespace AppColoreando.Infrastructure.Persistence;

internal static class StylePresetSeed
{
    private static readonly DateTime SeedDate = new(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc);

    internal static IReadOnlyCollection<StylePreset> Items =>
    [
        Preset("10000000-0000-0000-0000-000000000001", "Natural", "natural", 80, 12, .45, .55, .80, .10, .08),
        Preset("10000000-0000-0000-0000-000000000002", "Kids", "kids", 24, 6, .70, .45, .90, .22, .12),
        Preset("10000000-0000-0000-0000-000000000003", "Detailed", "detailed", 180, 16, .25, .70, .72, .08, .10),
        Preset("10000000-0000-0000-0000-000000000004", "Aura", "aura", 90, 12, .38, .58, .86, .28, .12),
        Preset("10000000-0000-0000-0000-000000000005", "Tesoro", "tesoro", 160, 16, .26, .68, .80, .18, .14),
        Preset("10000000-0000-0000-0000-000000000006", "Revela", "revela", 100, 12, .36, .60, .82, .14, .10),
        Preset("10000000-0000-0000-0000-000000000007", "Postal Viva", "postal-viva", 120, 14, .34, .62, .82, .24, .18),
        Preset("10000000-0000-0000-0000-000000000008", "Lumina", "lumina", 100, 12, .36, .58, .85, .34, .22),
        Preset("10000000-0000-0000-0000-000000000009", "Eclipse", "eclipse", 110, 12, .34, .64, .84, .30, .28),
        Preset("10000000-0000-0000-0000-000000000010", "Premium Animals", "animals", 650, 72, .30, .66, .84, .18, .12),
        Preset("10000000-0000-0000-0000-000000000011", "Premium Landscapes", "landscape", 800, 85, .28, .68, .82, .20, .14),
        Preset("10000000-0000-0000-0000-000000000012", "Premium Botanical", "botanical", 700, 80, .29, .66, .84, .24, .12),
        Preset("10000000-0000-0000-0000-000000000013", "Premium Food", "food", 550, 64, .32, .62, .86, .22, .12),
        Preset("10000000-0000-0000-0000-000000000014", "Premium Vehicles", "vehicles", 600, 70, .27, .72, .78, .15, .14),
        Preset("10000000-0000-0000-0000-000000000015", "Premium Space", "space", 750, 90, .26, .70, .80, .28, .18),
        Preset("10000000-0000-0000-0000-000000000016", "Premium Underwater", "underwater", 780, 88, .28, .68, .82, .25, .16),
        Preset("10000000-0000-0000-0000-000000000017", "Premium Architecture", "architecture", 650, 68, .25, .74, .76, .12, .16),
    ];

    private static StylePreset Preset(
        string id, string name, string code, int regions, int colors,
        double simplify, double edge, double smooth, double saturation, double contrast) =>
        new()
        {
            Id = Guid.Parse(id), Name = name, Code = code,
            TargetRegionCount = regions, MinRegionArea = 32, MaxColors = colors,
            SimplificationTolerance = simplify, EdgeSensitivity = edge,
            CurveSmoothness = smooth, SaturationBoost = saturation,
            ContrastBoost = contrast, SemanticMergeEnabled = true,
            IsActive = true, CreatedAtUtc = SeedDate
        };
}
