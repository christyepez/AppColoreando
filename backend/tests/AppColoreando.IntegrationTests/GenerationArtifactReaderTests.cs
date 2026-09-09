using System.Text.Json;
using AppColoreando.Infrastructure.ContentGeneration;

namespace AppColoreando.IntegrationTests;

public sealed class GenerationArtifactReaderTests
{
    [Fact]
    public async Task Reads_whitelisted_artifact_from_primary_variant_manifest()
    {
        var root = CreateTempDirectory();
        try
        {
            var variant = Path.Combine(root, "variants", "normal");
            Directory.CreateDirectory(variant);
            var preview = Path.Combine(variant, "catalog-preview.webp");
            await File.WriteAllBytesAsync(preview, [10, 20, 30]);
            var primaryManifest = Path.Combine(variant, "manifest.json");
            await WriteJsonAsync(primaryManifest, new { catalogPreviewPath = preview });
            var rootManifest = Path.Combine(root, "manifest.json");
            await WriteJsonAsync(rootManifest, new { type = "variant-pack", primaryManifestPath = primaryManifest });

            var result = await new FileSystemGenerationArtifactReader()
                .ReadAsync(rootManifest, "catalog", CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal("image/webp", result!.ContentType);
            Assert.Equal(new byte[] { 10, 20, 30 }, result.Content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Rejects_artifact_path_outside_generation_root()
    {
        var root = CreateTempDirectory();
        var outside = CreateTempDirectory();
        try
        {
            var preview = Path.Combine(outside, "catalog-preview.webp");
            await File.WriteAllBytesAsync(preview, [1, 2, 3]);
            var manifest = Path.Combine(root, "manifest.json");
            await WriteJsonAsync(manifest, new { catalogPreviewPath = preview });

            var result = await new FileSystemGenerationArtifactReader()
                .ReadAsync(manifest, "catalog", CancellationToken.None);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public async Task Rejects_unknown_artifact_kind()
    {
        var root = CreateTempDirectory();
        try
        {
            var manifest = Path.Combine(root, "manifest.json");
            await WriteJsonAsync(manifest, new { catalogPreviewPath = "catalog-preview.webp" });

            var result = await new FileSystemGenerationArtifactReader()
                .ReadAsync(manifest, "../../secret", CancellationToken.None);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"appcoloreando-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task WriteJsonAsync(string path, object payload)
    {
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload));
    }
}
