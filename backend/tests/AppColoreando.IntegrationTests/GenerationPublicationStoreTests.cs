using System.Text.Json;
using AppColoreando.Application.Contracts;
using AppColoreando.Infrastructure.ContentGeneration;
using Microsoft.Extensions.Configuration;

namespace AppColoreando.IntegrationTests;

public sealed class GenerationPublicationStoreTests
{
    [Fact]
    public async Task Materialize_applies_overlay_without_mutating_generation_bundle()
    {
        var root = CreateTempDirectory();
        var published = CreateTempDirectory();
        try
        {
            var manifest = await CreateBundleAsync(root);
            var originalBundle = await File.ReadAllTextAsync(
                Path.Combine(root, "variants", "normal", "bundle.json"));
            var store = CreateStore(published);
            var adjustment = new RegionAdjustmentDto(
                1, "#112233", "beak", "subject-accent", false,
                "review", Guid.NewGuid(), DateTime.UtcNow);
            var artworkId = Guid.NewGuid();
            var result = await store.MaterializeAsync(
                artworkId, manifest, [adjustment], CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result!.RegionCount);
            Assert.True(File.Exists(result.BundlePath));
            Assert.True(File.Exists(result.ThumbnailPath));
            Assert.Equal(originalBundle, await File.ReadAllTextAsync(
                Path.Combine(root, "variants", "normal", "bundle.json")));

            using var publishedBundle = JsonDocument.Parse(
                await File.ReadAllTextAsync(result.BundlePath));
            var region = publishedBundle.RootElement.GetProperty("regions")[0];
            Assert.Equal("beak", region.GetProperty("semanticTag").GetString());
            Assert.False(region.GetProperty("labelVisibleAtBase").GetBoolean());
            var overrideColorId = region.GetProperty("colorId").GetInt32();
            var palette = publishedBundle.RootElement.GetProperty("palette");
            Assert.Contains(palette.EnumerateArray(), x =>
                x.GetProperty("id").GetInt32() == overrideColorId &&
                x.GetProperty("hex").GetString() == "#112233");
            Assert.NotNull(await store.ReadPublishedAsync(artworkId, "bundle", CancellationToken.None));
            Assert.NotNull(await store.ReadPublishedAsync(artworkId, "thumbnail", CancellationToken.None));
            Assert.Null(await store.ReadPublishedAsync(artworkId, "../bundle", CancellationToken.None));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(published, recursive: true);
        }
    }

    [Fact]
    public async Task Materialize_rejects_primary_manifest_outside_generation_root()
    {
        var root = CreateTempDirectory();
        var outside = CreateTempDirectory();
        var published = CreateTempDirectory();
        try
        {
            var outsideManifest = Path.Combine(outside, "manifest.json");
            await File.WriteAllTextAsync(outsideManifest, "{}");
            var rootManifest = Path.Combine(root, "manifest.json");
            await File.WriteAllTextAsync(rootManifest, JsonSerializer.Serialize(new
            {
                type = "variant-pack",
                primaryManifestPath = outsideManifest
            }));
            var store = CreateStore(published);
            var result = await store.MaterializeAsync(
                Guid.NewGuid(), rootManifest, [], CancellationToken.None);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
            Directory.Delete(published, recursive: true);
        }
    }

    private static FileSystemGenerationPublicationStore CreateStore(string root)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ContentGeneration:PublishedRoot"] = root
            })
            .Build();
        return new FileSystemGenerationPublicationStore(configuration);
    }

    private static async Task<string> CreateBundleAsync(string root)
    {
        var variant = Path.Combine(root, "variants", "normal");
        Directory.CreateDirectory(variant);
        var bundlePath = Path.Combine(variant, "bundle.json");
        var thumbnailPath = Path.Combine(variant, "thumbnail.webp");
        var bundle = new
        {
            schemaVersion = "2.2",
            palette = new[] { new { id = 1, hex = "#FFAA00", name = "Amber" } },
            regions = new[] { new { id = 1, colorId = 1, semanticTag = "subject", semanticRole = "subject", labelVisibleAtBase = true } },
            adjustments = Array.Empty<object>()
        };
        await File.WriteAllTextAsync(bundlePath, JsonSerializer.Serialize(bundle));
        await File.WriteAllBytesAsync(thumbnailPath, [1, 2, 3, 4]);
        var primaryManifest = Path.Combine(variant, "manifest.json");
        await File.WriteAllTextAsync(primaryManifest, JsonSerializer.Serialize(new
        {
            bundlePath,
            thumbnailPath
        }));
        var rootManifest = Path.Combine(root, "manifest.json");
        await File.WriteAllTextAsync(rootManifest, JsonSerializer.Serialize(new
        {
            type = "variant-pack",
            primaryManifestPath = primaryManifest
        }));
        return rootManifest;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"appcoloreando-publish-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
