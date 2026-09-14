using System.Text.Json;
using AppColoreando.Application.Contracts;
using AppColoreando.Infrastructure.ContentGeneration;

namespace AppColoreando.IntegrationTests;

public sealed class GenerationAdjustmentStoreTests
{
    [Fact]
    public async Task Upsert_and_delete_preserve_generated_bundle()
    {
        var root = CreateTempDirectory();
        try
        {
            var manifest = await CreateBundleAsync(root);
            var store = new FileSystemGenerationAdjustmentStore();
            var userId = Guid.NewGuid();

            var saved = await store.UpsertAsync(manifest, 2, userId,
                new RegionAdjustmentRequest("#12abEF", "beak", "subject-accent", false, "Refined beak"),
                CancellationToken.None);
            Assert.NotNull(saved);
            Assert.Equal("#12ABEF", saved!.ColorHex);
            Assert.Equal("beak", saved.SemanticTag);
            Assert.False(saved.LabelVisibleAtBase);
            Assert.True(File.Exists(Path.Combine(root, "adjustments.json")));

            var regionsBefore = await File.ReadAllTextAsync(
                Path.Combine(root, "variants", "normal", "regions.json"));
            var listed = await store.ListAsync(manifest, CancellationToken.None);
            Assert.Single(listed);

            Assert.True(await store.DeleteAsync(manifest, 2, CancellationToken.None));
            Assert.Empty(await store.ListAsync(manifest, CancellationToken.None));
            var regionsAfter = await File.ReadAllTextAsync(
                Path.Combine(root, "variants", "normal", "regions.json"));
            Assert.Equal(regionsBefore, regionsAfter);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Rejects_invalid_color_and_unknown_region()
    {
        var root = CreateTempDirectory();
        try
        {
            var manifest = await CreateBundleAsync(root);
            var store = new FileSystemGenerationAdjustmentStore();

            await Assert.ThrowsAsync<ArgumentException>(() => store.UpsertAsync(
                manifest, 1, Guid.NewGuid(),
                new RegionAdjustmentRequest("red", null, null, null, null),
                CancellationToken.None));

            var missing = await store.UpsertAsync(
                manifest, 99, Guid.NewGuid(),
                new RegionAdjustmentRequest("#112233", null, null, null, null),
                CancellationToken.None);
            Assert.Null(missing);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task Rejects_primary_manifest_outside_generation_root()
    {
        var root = CreateTempDirectory();
        var outside = CreateTempDirectory();
        try
        {
            var outsideManifest = Path.Combine(outside, "manifest.json");
            await File.WriteAllTextAsync(outsideManifest, JsonSerializer.Serialize(new { regionsPath = Path.Combine(outside, "regions.json") }));
            await File.WriteAllTextAsync(Path.Combine(outside, "regions.json"), "[{\"id\":1}]");
            var rootManifest = Path.Combine(root, "manifest.json");
            await File.WriteAllTextAsync(rootManifest, JsonSerializer.Serialize(new { type = "variant-pack", primaryManifestPath = outsideManifest }));
            var store = new FileSystemGenerationAdjustmentStore();
            var result = await store.UpsertAsync(rootManifest, 1, Guid.NewGuid(), new RegionAdjustmentRequest("#112233", null, null, null, null), CancellationToken.None);
            Assert.Null(result);
            Assert.False(File.Exists(Path.Combine(root, "adjustments.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
    }

    private static async Task<string> CreateBundleAsync(string root)
    {
        var variant = Path.Combine(root, "variants", "normal");
        Directory.CreateDirectory(variant);
        var regionsPath = Path.Combine(variant, "regions.json");
        await File.WriteAllTextAsync(regionsPath, JsonSerializer.Serialize(new[]
        {
            new { id = 1, colorId = 1 },
            new { id = 2, colorId = 2 }
        }));

        var primaryManifest = Path.Combine(variant, "manifest.json");
        await File.WriteAllTextAsync(primaryManifest,
            JsonSerializer.Serialize(new { regionsPath }));
        var rootManifest = Path.Combine(root, "manifest.json");
        await File.WriteAllTextAsync(rootManifest,
            JsonSerializer.Serialize(new { type = "variant-pack", primaryManifestPath = primaryManifest }));
        return rootManifest;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"appcoloreando-adjust-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}

