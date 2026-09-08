using AppColoreando.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppColoreando.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        {
            await db.Database.MigrateAsync(ct);
        }
        if (configuration.GetValue("Seed:Enabled", true))
        {
            await SeedAsync(scope.ServiceProvider, db, configuration, ct);
        }
    }

    private static async Task SeedAsync(IServiceProvider services, AppDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        foreach (var country in new[] { ("EC", "Ecuador"), ("CO", "Colombia"), ("SA", "South America"), ("US", "United States") })
        {
            if (!await db.Countries.AnyAsync(x => x.Code == country.Item1, ct)) db.Countries.Add(new Country { Code = country.Item1, Name = country.Item2 });
        }

        var categories = new[] { ("Landscapes", "landscapes"), ("Culture", "culture"), ("Geometry", "geometry"), ("Animals", "animals") };
        foreach (var category in categories)
        {
            if (!await db.Categories.AnyAsync(x => x.Slug == category.Item2, ct)) db.Categories.Add(new Category { Name = category.Item1, Slug = category.Item2 });
        }

        foreach (var brand in new[] { "licensed-brand-placeholder", "classic-animation-placeholder" })
        {
            if (!await db.Brands.AnyAsync(x => x.Slug == brand, ct)) db.Brands.Add(new Brand { Name = brand.Replace('-', ' '), Slug = brand, Enabled = false, Notes = "Rights placeholder only. No unlicensed artwork is seeded." });
        }

        await db.SaveChangesAsync(ct);
        var geometry = await db.Categories.SingleAsync(x => x.Slug == "geometry", ct);
        if (!await db.Artworks.AnyAsync(x => x.Title == "Andean Geometry Demo", ct))
        {
            db.Artworks.Add(new Artwork
            {
                Title = "Andean Geometry Demo",
                Description = "Original deterministic geometric color-by-number demo.",
                CategoryId = geometry.Id,
                CountryCode = "EC",
                LicenseType = "Original",
                ThumbnailUrl = "/assets/demo/andean-geometry-thumb.png",
                AssetUrl = "/assets/demo/andean-geometry.bundle.json",
                BundleChecksum = "seed-demo",
                Difficulty = 2,
                RegionCount = 36,
                PublishingStatus = PublishingStatus.Published,
                PublishedAtUtc = DateTime.UtcNow,
                Assets = { new ArtworkAsset { Kind = ArtworkAssetKind.BundleJson, Uri = "/assets/demo/andean-geometry.bundle.json", ContentType = "application/json", IsPrimary = true } }
            });
        }

        foreach (var collection in new[] { ("Ecuador Originals", "ecuador-originals", "EC"), ("Colombia Originals", "colombia-originals", "CO"), ("South America Starter", "south-america-starter", "SA"), ("USA Starter", "usa-starter", "US") })
        {
            if (!await db.Collections.AnyAsync(x => x.Slug == collection.Item2, ct)) db.Collections.Add(new Collection { Name = collection.Item1, Slug = collection.Item2, CountryCode = collection.Item3, Description = "Safe original demo collection." });
        }

        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword) && !await db.Users.AnyAsync(x => x.Email == adminEmail.ToLower(), ct))
        {
            var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();
            var admin = new AppUser { Email = adminEmail.Trim().ToLowerInvariant(), DisplayName = "Administrator", Role = AppRole.Admin.ToString() };
            admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
            db.Users.Add(admin);
            db.UserProfiles.Add(new UserProfile { UserId = admin.Id, Locale = "es" });
        }

        await db.SaveChangesAsync(ct);
    }
}
