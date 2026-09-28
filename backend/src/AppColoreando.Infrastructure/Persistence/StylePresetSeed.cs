using AppColoreando.Domain.Entities;

namespace AppColoreando.Infrastructure.Persistence;

internal static class StylePresetSeed
{
    private static readonly DateTime SeedDate = new(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc);

    internal static IReadOnlyCollection<StylePreset> Items =>
    [
        Preset("10000000-0000-0000-0000-000000000001", "Natural", "natural", 300, 70, .36, .64, .82, .16, .12),
        Preset("10000000-0000-0000-0000-000000000002", "Kids", "kids", 40, 16, .70, .45, .90, .22, .12),
        Preset("10000000-0000-0000-0000-000000000003", "Detailed", "detailed", 625, 120, .18, .78, .68, .14, .14),
        Preset("10000000-0000-0000-0000-000000000004", "Aura", "aura", 320, 70, .30, .66, .84, .32, .16),
        Preset("10000000-0000-0000-0000-000000000005", "Tesoro", "tesoro", 550, 110, .20, .76, .76, .22, .18),
        Preset("10000000-0000-0000-0000-000000000006", "Revela", "revela", 350, 80, .28, .68, .80, .18, .14),
        Preset("10000000-0000-0000-0000-000000000007", "Postal Viva", "postal-viva", 400, 90, .26, .70, .78, .28, .22),
        Preset("10000000-0000-0000-0000-000000000008", "Lumina", "lumina", 350, 80, .28, .66, .82, .38, .26),
        Preset("10000000-0000-0000-0000-000000000009", "Eclipse", "eclipse", 380, 90, .26, .72, .80, .34, .32),
        Preset("10000000-0000-0000-0000-000000000010", "Andino", "andino", 420, 84, .24, .72, .82, .30, .20),
        Preset("10000000-0000-0000-0000-000000000011", "Andino Vivo", "andino-vivo", 650, 120, .18, .78, .84, .38, .24),
        Preset("10000000-0000-0000-0000-000000000012", "Galactico", "galactico", 520, 110, .20, .76, .86, .42, .30),
        Preset("10000000-0000-0000-0000-000000000013", "Espacio Epico", "space-opera", 625, 150, .16, .82, .88, .46, .34),
        Preset("10000000-0000-0000-0000-000000000014", "Comic Clasico", "comic-classic", 420, 72, .24, .70, .84, .38, .28),
        Preset("10000000-0000-0000-0000-000000000015", "Comic Pop", "comic-pop", 560, 110, .18, .78, .88, .48, .32),
        Preset("10000000-0000-0000-0000-000000000016", "Anime Clasico", "anime-classic", 480, 90, .22, .74, .86, .36, .26),
        Preset("10000000-0000-0000-0000-000000000017", "Anime Epico", "anime-epic", 625, 140, .16, .82, .88, .44, .32),
        Preset("10000000-0000-0000-0000-000000000018", "Chibi", "chibi", 180, 40, .40, .58, .90, .40, .22),
        Preset("10000000-0000-0000-0000-000000000019", "Mecha", "mecha", 625, 150, .14, .84, .88, .40, .34),
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
