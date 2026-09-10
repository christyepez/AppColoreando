using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.Extensions.Configuration;

namespace AppColoreando.Infrastructure.ContentGeneration;

public sealed class FileSystemGenerationPublicationStore(IConfiguration configuration)
    : IGenerationPublicationStore
{
    private readonly string root = Path.GetFullPath(
        configuration["ContentGeneration:PublishedRoot"]
        ?? Path.Combine(AppContext.BaseDirectory, "content-data", "published"));

    public async Task<GenerationPublicationAssets?> MaterializeAsync(
        Guid artworkId,
        string resultManifestPath,
        IReadOnlyCollection<RegionAdjustmentDto> adjustments,
        CancellationToken ct)
    {
        var generationRoot = Path.GetDirectoryName(Path.GetFullPath(resultManifestPath))!;
        var primaryManifest = await ResolvePrimaryManifestAsync(resultManifestPath, ct);
        if (!IsWithin(generationRoot, primaryManifest) || !File.Exists(primaryManifest)) return null;
        await using var manifestStream = File.OpenRead(primaryManifest);
        using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        if (!manifest.RootElement.TryGetProperty("bundlePath", out var bundleNode) ||
            !manifest.RootElement.TryGetProperty("thumbnailPath", out var thumbNode))
            return null;

        var sourceBundle = Path.GetFullPath(bundleNode.GetString() ?? string.Empty);
        var sourceThumbnail = Path.GetFullPath(thumbNode.GetString() ?? string.Empty);
        if (!IsWithin(generationRoot, sourceBundle) || !IsWithin(generationRoot, sourceThumbnail) ||
            !File.Exists(sourceBundle) || !File.Exists(sourceThumbnail))
            return null;

        var node = JsonNode.Parse(await File.ReadAllTextAsync(sourceBundle, ct))?.AsObject();
        if (node is null) return null;
        ApplyAdjustments(node, adjustments);

        var targetDirectory = Path.Combine(root, artworkId.ToString("N"));
        Directory.CreateDirectory(targetDirectory);
        var targetBundle = Path.Combine(targetDirectory, "bundle.json");
        var targetThumbnail = Path.Combine(targetDirectory, "thumbnail.webp");
        var json = node.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(targetBundle, json, ct);
        File.Copy(sourceThumbnail, targetThumbnail, overwrite: true);

        var checksum = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(targetBundle, ct)))
            .ToLowerInvariant();
        var regionCount = node["regions"]?.AsArray().Count ?? 0;
        return new GenerationPublicationAssets(targetBundle, targetThumbnail, regionCount, checksum);
    }

    public async Task<GenerationArtifactDto?> ReadPublishedAsync(
        Guid artworkId, string artifactKind, CancellationToken ct)
    {
        var fileName = artifactKind.ToLowerInvariant() switch
        {
            "bundle" => "bundle.json",
            "thumbnail" => "thumbnail.webp",
            _ => null
        };
        if (fileName is null) return null;
        var directory = Path.Combine(root, artworkId.ToString("N"));
        var path = Path.GetFullPath(Path.Combine(directory, fileName));
        if (!IsWithin(root, path) || !File.Exists(path)) return null;
        var bytes = await File.ReadAllBytesAsync(path, ct);
        return new GenerationArtifactDto(bytes,
            fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "application/json" : "image/webp",
            fileName);
    }
    private static void ApplyAdjustments(
        JsonObject bundle,
        IReadOnlyCollection<RegionAdjustmentDto> adjustments)
    {
        var regions = bundle["regions"]?.AsArray();
        var palette = bundle["palette"]?.AsArray();
        if (regions is null || palette is null || adjustments.Count == 0) return;

        foreach (var adjustment in adjustments)
        {
            var region = regions.OfType<JsonObject>()
                .FirstOrDefault(x => x["id"]?.GetValue<int>() == adjustment.RegionId);
            if (region is null) continue;

            if (!string.IsNullOrWhiteSpace(adjustment.ColorHex))
                region["colorId"] = ResolveOverrideColorId(palette, adjustment.ColorHex!);
            if (!string.IsNullOrWhiteSpace(adjustment.SemanticTag))
                region["semanticTag"] = adjustment.SemanticTag;
            if (!string.IsNullOrWhiteSpace(adjustment.SemanticRole))
                region["semanticRole"] = adjustment.SemanticRole;
            if (adjustment.LabelVisibleAtBase.HasValue)
                region["labelVisibleAtBase"] = adjustment.LabelVisibleAtBase.Value;
        }
        bundle["adjustments"] = JsonSerializer.SerializeToNode(adjustments);
    }
    private static int ResolveOverrideColorId(JsonArray palette, string colorHex)
    {
        foreach (var item in palette.OfType<JsonObject>())
        {
            if (string.Equals(item["hex"]?.GetValue<string>(), colorHex, StringComparison.OrdinalIgnoreCase))
                return item["id"]?.GetValue<int>() ?? 1;
        }

        var nextId = palette.OfType<JsonObject>()
            .Select(x => x["id"]?.GetValue<int>() ?? 0)
            .DefaultIfEmpty(0).Max() + 1;
        palette.Add(new JsonObject
        {
            ["id"] = nextId,
            ["hex"] = colorHex.ToUpperInvariant(),
            ["name"] = "Manual override"
        });
        return nextId;
    }

    private static async Task<string> ResolvePrimaryManifestAsync(
        string resultManifestPath, CancellationToken ct)
    {
        var rootManifest = Path.GetFullPath(resultManifestPath);
        await using var stream = File.OpenRead(rootManifest);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (document.RootElement.TryGetProperty("type", out var type) &&
            type.GetString() == "variant-pack" &&
            document.RootElement.TryGetProperty("primaryManifestPath", out var primary))
            return Path.GetFullPath(primary.GetString() ?? rootManifest);
        return rootManifest;
    }
    private static bool IsWithin(string rootDirectory, string candidate)
    {
        var rootPath = Path.GetFullPath(rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return Path.GetFullPath(candidate).StartsWith(rootPath, comparison);
    }
}
