using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AppColoreando.Api.Domain;
using AppColoreando.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=postgres;Port=5432;Database=appcoloreando;Username=appcoloreando;Password=appcoloreando";
var jwtKey = builder.Configuration["Jwt:Key"] ?? "LOCAL-ONLY-CHANGE-THIS-KEY-BEFORE-PRODUCTION-123456";

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddNpgSql(connectionString);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "AppColoreando",
        ValidAudience = "AppColoreando.MobileAdmin",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapPost("/api/auth/register", async (RegisterRequest request, AppDbContext db, IPasswordHasher<AppUser> hasher) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(x => x.Email == email)) return Results.Conflict(new { message = "Email already registered" });
    var user = new AppUser { Email = email, DisplayName = request.DisplayName.Trim(), PasswordHash = string.Empty };
    user.PasswordHash = hasher.HashPassword(user, request.Password);
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/users/{user.Id}", new { user.Id, user.Email, user.DisplayName });
});

app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db, IPasswordHasher<AppUser> hasher) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email && x.IsActive);
    if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    user.LastLoginAtUtc = DateTime.UtcNow;
    db.UserActivityHistory.Add(new UserActivityHistory { UserId = user.Id, ActivityType = "Login" });
    await UpsertDailyMetric(db, user.Id, sessions: 1);
    await db.SaveChangesAsync();

    var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role) };
    var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken("AppColoreando", "AppColoreando.MobileAdmin", claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials);
    return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), user = new { user.Id, user.Email, user.DisplayName, user.Role } });
});

app.MapGet("/api/catalog/artworks", async (AppDbContext db) =>
    Results.Ok(await db.Artworks.AsNoTracking().Where(x => x.IsPublished).Include(x => x.Category).OrderByDescending(x => x.PublishedAtUtc).ToListAsync()));

app.MapGet("/api/me/progress", [Authorize] async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = CurrentUserId(principal);
    return Results.Ok(await db.UserArtworkProgress.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.LastOpenedAtUtc).ToListAsync());
});

app.MapPut("/api/me/artworks/{artworkId:guid}/progress", [Authorize] async (Guid artworkId, ProgressRequest request, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = CurrentUserId(principal);
    if (!await db.Artworks.AnyAsync(x => x.Id == artworkId && x.IsPublished)) return Results.NotFound();
    var progress = await db.UserArtworkProgress.SingleOrDefaultAsync(x => x.UserId == userId && x.ArtworkId == artworkId);
    if (progress is null)
    {
        progress = new UserArtworkProgress { UserId = userId, ArtworkId = artworkId };
        db.UserArtworkProgress.Add(progress);
    }
    progress.CompletionPercent = Math.Clamp(request.CompletionPercent, 0, 100);
    progress.CompletedRegionIdsJson = JsonSerializer.Serialize(request.CompletedRegionIds.Distinct());
    progress.LastOpenedAtUtc = DateTime.UtcNow;
    progress.IsFavorite = request.IsFavorite;
    if (progress.CompletionPercent >= 100 && progress.CompletedAtUtc is null) progress.CompletedAtUtc = DateTime.UtcNow;
    db.UserActivityHistory.Add(new UserActivityHistory { UserId = userId, ArtworkId = artworkId, ActivityType = progress.CompletionPercent >= 100 ? "ArtworkCompleted" : "ProgressSaved", MetadataJson = JsonSerializer.Serialize(new { progress.CompletionPercent, regionCount = request.CompletedRegionIds.Count }) });
    await UpsertDailyMetric(db, userId, opened: 1, completed: progress.CompletionPercent >= 100 ? 1 : 0, regions: request.CompletedRegionIds.Count);
    await db.SaveChangesAsync();
    return Results.Ok(progress);
});

app.MapGet("/api/me/history", [Authorize] async (ClaimsPrincipal principal, AppDbContext db) =>
    Results.Ok(await db.UserActivityHistory.AsNoTracking().Where(x => x.UserId == CurrentUserId(principal)).OrderByDescending(x => x.OccurredAtUtc).Take(250).ToListAsync()));

app.MapGet("/api/me/metrics", [Authorize] async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = CurrentUserId(principal);
    var daily = await db.UserMetricsDaily.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.MetricDate).Take(90).ToListAsync();
    return Results.Ok(new { daily, totals = new { sessions = daily.Sum(x => x.Sessions), artworksOpened = daily.Sum(x => x.ArtworksOpened), artworksCompleted = daily.Sum(x => x.ArtworksCompleted), regionsColored = daily.Sum(x => x.RegionsColored), activeSeconds = daily.Sum(x => x.ActiveSeconds) } });
});

app.MapPost("/api/admin/categories", [Authorize(Policy = "AdminOnly")] async (Category request, ClaimsPrincipal principal, AppDbContext db) =>
{
    request.Id = Guid.NewGuid(); request.CreatedByUserId = CurrentUserId(principal); db.Categories.Add(request); await db.SaveChangesAsync(); return Results.Created($"/api/admin/categories/{request.Id}", request);
});

app.MapPost("/api/admin/artworks", [Authorize(Policy = "AdminOnly")] async (Artwork request, ClaimsPrincipal principal, AppDbContext db) =>
{
    request.Id = Guid.NewGuid(); request.CreatedByUserId = CurrentUserId(principal); request.PublishedAtUtc = request.IsPublished ? DateTime.UtcNow : null; db.Artworks.Add(request); await AddAudit(db, principal, "Artwork", request.Id, "Create", request); await db.SaveChangesAsync(); return Results.Created($"/api/admin/artworks/{request.Id}", request);
});

app.MapPut("/api/admin/artworks/{id:guid}", [Authorize(Policy = "AdminOnly")] async (Guid id, Artwork request, ClaimsPrincipal principal, AppDbContext db) =>
{
    var entity = await db.Artworks.FindAsync(id); if (entity is null) return Results.NotFound();
    entity.Title = request.Title; entity.Description = request.Description; entity.CategoryId = request.CategoryId; entity.CountryCode = request.CountryCode; entity.Brand = request.Brand; entity.LicenseType = request.LicenseType; entity.LicenseReference = request.LicenseReference; entity.ThumbnailUrl = request.ThumbnailUrl; entity.AssetUrl = request.AssetUrl; entity.Difficulty = request.Difficulty; entity.RegionCount = request.RegionCount; entity.IsPublished = request.IsPublished; entity.PublishedAtUtc ??= request.IsPublished ? DateTime.UtcNow : null; entity.UpdatedAtUtc = DateTime.UtcNow; entity.UpdatedByUserId = CurrentUserId(principal);
    await AddAudit(db, principal, "Artwork", id, "Update", request); await db.SaveChangesAsync(); return Results.Ok(entity);
});

app.MapGet("/api/admin/users", [Authorize(Policy = "AdminOnly")] async (AppDbContext db) => Results.Ok(await db.Users.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync()));
app.MapGet("/api/admin/audit", [Authorize(Policy = "AdminOnly")] async (AppDbContext db) => Results.Ok(await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(500).ToListAsync()));
app.MapGet("/api/admin/metrics", [Authorize(Policy = "AdminOnly")] async (AppDbContext db) => Results.Ok(new { users = await db.Users.CountAsync(), publishedArtworks = await db.Artworks.CountAsync(x => x.IsPublished), completions = await db.UserArtworkProgress.CountAsync(x => x.CompletedAtUtc != null), activityEvents = await db.UserActivityHistory.CountAsync() }));

app.Run();

static Guid CurrentUserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
static async Task AddAudit(AppDbContext db, ClaimsPrincipal p, string entity, Guid id, string action, object changes) => await db.AuditLogs.AddAsync(new AuditLog { UserId = CurrentUserId(p), EntityName = entity, EntityId = id.ToString(), Action = action, ChangesJson = JsonSerializer.Serialize(changes) });
static async Task UpsertDailyMetric(AppDbContext db, Guid userId, int sessions = 0, int opened = 0, int completed = 0, int regions = 0)
{
    var date = DateOnly.FromDateTime(DateTime.UtcNow); var metric = await db.UserMetricsDaily.SingleOrDefaultAsync(x => x.UserId == userId && x.MetricDate == date);
    if (metric is null) { metric = new UserMetricDaily { UserId = userId, MetricDate = date }; db.UserMetricsDaily.Add(metric); }
    metric.Sessions += sessions; metric.ArtworksOpened += opened; metric.ArtworksCompleted += completed; metric.RegionsColored += regions;
}

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record ProgressRequest(decimal CompletionPercent, List<int> CompletedRegionIds, bool IsFavorite);
