using System.Security.Cryptography;
using System.Text.Json;
using AppColoreando.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AppColoreando.Infrastructure.Persistence;

internal static class StarterCatalogSeed
{
    private static readonly DateTime PublishedAtUtc =
        new(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);

    private sealed record StarterSpec(
        Guid ArtworkId,
        string Folder,
        string Title,
        string Description,
        string CategorySlug,
        string CollectionSlug,
        string CountryCode,
        int Difficulty);

    private static readonly StarterSpec[] Items =
    [
        new(
            Guid.Parse("a1111111-1111-4111-8111-111111111111"),
            "andean",
            "Andean Summit & Lagoon",
            "Original AppColoreando Andean landscape starter artwork.",
            "andean-landscapes",
            "andes-ecuador",
            "EC",
            3),
        new(
            Guid.Parse("a2222222-2222-4222-8222-222222222222"),
            "space-opera",
            "Twin Suns Patrol",
            "Original AppColoreando space-opera starter artwork.",
            "space-opera",
            "space-opera-originals",
            "SA",
            3),
        new(
            Guid.Parse("a3333333-3333-4333-8333-333333333333"),
            "comic",
            "Urban Guardian",
            "Original AppColoreando comic starter artwork.",
            "comic-heroes",
            "comic-originals",
            "SA",
            3),
        new(
            Guid.Parse("a4444444-4444-4444-8444-444444444444"),
            "anime",
            "Skyline Adventurer",
            "Original AppColoreando anime starter artwork.",
            "anime-adventure",
            "anime-originals",
            "SA",
            3),
        new(
            Guid.Parse("a5555555-5555-4555-8555-555555555555"),
            "mecha",
            "Neon Sentinel Mecha",
            "Original AppColoreando mecha starter artwork.",
            "anime-mecha",
            "mecha-originals",
            "SA",
            3),
    ];

    internal static async Task SeedAsync(
        AppDbContext db,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (!configuration.GetValue("Seed:StarterCatalogEnabled", true))
            return;

        var sourceRoot = Path.GetFullPath(
            configuration["Seed:StarterCatalogRoot"]
            ?? Path.Combine(AppContext.BaseDirectory, "starter-catalog"));
        if (!Directory.Exists(sourceRoot))
            return;

        var publishedRoot = Path.GetFullPath(
            configuration["ContentGeneration:PublishedRoot"]
            ?? Path.Combine(AppContext.BaseDirectory, "content-data", "published"));
        Directory.CreateDirectory(publishedRoot);

        foreach (var spec in Items)
        {
            ct.ThrowIfCancellationRequested();

            var category = await db.Categories
                .SingleAsync(x => x.Slug == spec.CategorySlug, ct);
            var collection = await db.Collections
                .SingleAsync(x => x.Slug == spec.CollectionSlug, ct);

            var sourceDirectory = Path.Combine(sourceRoot, spec.Folder);
            var sourceBundle = Path.Combine(sourceDirectory, "bundle.json");
            var sourceThumbnail = Path.Combine(sourceDirectory, "thumbnail.webp");
            if (!File.Exists(sourceBundle) || !File.Exists(sourceThumbnail))
                throw new InvalidOperationException(
                    $"Starter catalog assets are incomplete for '{spec.Folder}'.");

            var targetDirectory = Path.Combine(
                publishedRoot,
                spec.ArtworkId.ToString("N"));
            Directory.CreateDirectory(targetDirectory);

            var targetBundle = Path.Combine(targetDirectory, "bundle.json");
            var targetThumbnail = Path.Combine(targetDirectory, "thumbnail.webp");
            File.Copy(sourceBundle, targetBundle, overwrite: true);
            File.Copy(sourceThumbnail, targetThumbnail, overwrite: true);

            var bundleBytes = await File.ReadAllBytesAsync(targetBundle, ct);
            var checksum = Convert.ToHexString(SHA256.HashData(bundleBytes))
                .ToLowerInvariant();
            var regionCount = CountRegions(bundleBytes);

            var bundleUrl = $"/api/catalog/artworks/{spec.ArtworkId}/bundle";
            var thumbnailUrl = $"/api/catalog/artworks/{spec.ArtworkId}/thumbnail";

            var artwork = await db.Artworks
                .Include(x => x.Assets)
                .SingleOrDefaultAsync(x => x.Id == spec.ArtworkId, ct);
            if (artwork is null)
            {
                artwork = new Artwork
                {
                    Id = spec.ArtworkId,
                    CreatedAtUtc = PublishedAtUtc
                };
                db.Artworks.Add(artwork);
            }

            artwork.Title = spec.Title;
            artwork.Description = spec.Description;
            artwork.CategoryId = category.Id;
            artwork.CountryCode = spec.CountryCode;
            artwork.LicenseType = "Original";
            artwork.LicenseReference = "starter-catalog";
            artwork.ThumbnailUrl = thumbnailUrl;
            artwork.AssetUrl = bundleUrl;
            artwork.BundleChecksum = checksum;
            artwork.Difficulty = spec.Difficulty;
            artwork.RegionCount = regionCount;
            artwork.PublishingStatus = PublishingStatus.Published;
            artwork.ScheduledPublishAtUtc = null;
            artwork.PublishedAtUtc = PublishedAtUtc;

            UpsertAsset(
                artwork,
                ArtworkAssetKind.BundleJson,
                bundleUrl,
                "application/json",
                checksum,
                new FileInfo(targetBundle).Length);
            UpsertAsset(
                artwork,
                ArtworkAssetKind.Thumbnail,
                thumbnailUrl,
                "image/webp",
                null,
                new FileInfo(targetThumbnail).Length);

            if (!await db.CollectionArtworks.AnyAsync(
                    x => x.CollectionId == collection.Id
                         && x.ArtworkId == spec.ArtworkId,
                    ct))
            {
                var nextSortOrder = await db.CollectionArtworks
                    .Where(x => x.CollectionId == collection.Id)
                    .Select(x => (int?)x.SortOrder)
                    .MaxAsync(ct) ?? -1;

                db.CollectionArtworks.Add(new CollectionArtwork
                {
                    CollectionId = collection.Id,
                    ArtworkId = spec.ArtworkId,
                    SortOrder = nextSortOrder + 1
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static int CountRegions(byte[] bundleBytes)
    {
        using var document = JsonDocument.Parse(bundleBytes);
        return document.RootElement.TryGetProperty("regions", out var regions)
            && regions.ValueKind == JsonValueKind.Array
            ? regions.GetArrayLength()
            : 0;
    }

    private static void UpsertAsset(
        Artwork artwork,
        ArtworkAssetKind kind,
        string uri,
        string contentType,
        string? checksum,
        long sizeBytes)
    {
        var asset = artwork.Assets.FirstOrDefault(x => x.Kind == kind);
        if (asset is null)
        {
            asset = new ArtworkAsset
            {
                ArtworkId = artwork.Id,
                Kind = kind
            };
            artwork.Assets.Add(asset);
        }

        asset.Uri = uri;
        asset.ContentType = contentType;
        asset.Checksum = checksum;
        asset.SizeBytes = sizeBytes;
        asset.IsPrimary = true;
    }
}
