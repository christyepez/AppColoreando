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
var connectionString = builder.Configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
if (jwtKey.Length < 32) throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer = "AppColoreando", ValidAudience = "AppColoreando.MobileAdmin",
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
});
builder.Services.AddAuthorizationBuilder().AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI(); app.UseCors(); app.UseAuthentication(); app.UseAuthorization(); app.MapHealthChecks("/health");
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

app.MapPost("/api/auth/register", async (RegisterRequest r, AppDbContext db, IPasswordHasher<AppUser> hasher) =>
{
    var email = r.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(x => x.Email == email)) return Results.Conflict(new { message = "Email already registered" });
    if (r.Password.Length < 10) return Results.BadRequest(new { message = "Password must contain at least 10 characters" });
    var user = new AppUser { Email = email, DisplayName = r.DisplayName.Trim(), PasswordHash = string.Empty };
    user.PasswordHash = hasher.HashPassword(user, r.Password); db.Users.Add(user); await db.SaveChangesAsync();
    return Results.Created($"/api/admin/users/{user.Id}", new { user.Id, user.Email, user.DisplayName });
});

app.MapPost("/api/auth/login", async (LoginRequest r, AppDbContext db, IPasswordHasher<AppUser> hasher) =>
{
    var user = await db.Users.SingleOrDefaultAsync(x => x.Email == r.Email.Trim().ToLowerInvariant() && x.IsActive);
    if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, r.Password) == PasswordVerificationResult.Failed) return Results.Unauthorized();
    user.LastLoginAtUtc = DateTime.UtcNow; db.UserActivityHistory.Add(new UserActivityHistory { UserId = user.Id, ActivityType = "Login" }); await UpsertMetric(db, user.Id, sessions: 1); await db.SaveChangesAsync();
    var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role) };
    var token = new JwtSecurityToken("AppColoreando", "AppColoreando.MobileAdmin", claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256));
    return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), user = new { user.Id, user.Email, user.DisplayName, user.Role } });
});

app.MapGet("/api/catalog/artworks", async (AppDbContext db) => Results.Ok(await db.Artworks.AsNoTracking().Where(x => x.IsPublished).Include(x => x.Category).OrderByDescending(x => x.PublishedAtUtc).ToListAsync()));

app.MapGet("/api/me/progress", [Authorize] async (ClaimsPrincipal p, AppDbContext db) => Results.Ok(await db.UserArtworkProgress.AsNoTracking().Where(x => x.UserId == UserId(p)).OrderByDescending(x => x.LastOpenedAtUtc).ToListAsync()));
app.MapPut("/api/me/artworks/{artworkId:guid}/progress", [Authorize] async (Guid artworkId, ProgressRequest r, ClaimsPrincipal p, AppDbContext db) =>
{
    var userId = UserId(p); if (!await db.Artworks.AnyAsync(x => x.Id == artworkId && x.IsPublished)) return Results.NotFound();
    var item = await db.UserArtworkProgress.SingleOrDefaultAsync(x => x.UserId == userId && x.ArtworkId == artworkId) ?? new UserArtworkProgress { UserId = userId, ArtworkId = artworkId };
    if (db.Entry(item).State == EntityState.Detached) db.UserArtworkProgress.Add(item);
    item.CompletionPercent = Math.Clamp(r.CompletionPercent, 0, 100); item.CompletedRegionIdsJson = JsonSerializer.Serialize(r.CompletedRegionIds.Distinct()); item.LastOpenedAtUtc = DateTime.UtcNow; item.IsFavorite = r.IsFavorite;
    if (item.CompletionPercent >= 100 && item.CompletedAtUtc is null) item.CompletedAtUtc = DateTime.UtcNow;
    db.UserActivityHistory.Add(new UserActivityHistory { UserId = userId, ArtworkId = artworkId, ActivityType = item.CompletedAtUtc is null ? "ProgressSaved" : "ArtworkCompleted", MetadataJson = JsonSerializer.Serialize(new { item.CompletionPercent, regionCount = r.CompletedRegionIds.Count }) });
    await UpsertMetric(db, userId, opened: 1, completed: item.CompletedAtUtc is null ? 0 : 1, regions: r.CompletedRegionIds.Count); await db.SaveChangesAsync(); return Results.Ok(item);
});
app.MapGet("/api/me/history", [Authorize] async (ClaimsPrincipal p, AppDbContext db) => Results.Ok(await db.UserActivityHistory.AsNoTracking().Where(x => x.UserId == UserId(p)).OrderByDescending(x => x.OccurredAtUtc).Take(250).ToListAsync()));
app.MapGet("/api/me/metrics", [Authorize] async (ClaimsPrincipal p, AppDbContext db) =>
{
    var daily = await db.UserMetricsDaily.AsNoTracking().Where(x => x.UserId == UserId(p)).OrderByDescending(x => x.MetricDate).Take(90).ToListAsync();
    return Results.Ok(new { daily, totals = new { sessions = daily.Sum(x => x.Sessions), artworksOpened = daily.Sum(x => x.ArtworksOpened), artworksCompleted = daily.Sum(x => x.ArtworksCompleted), regionsColored = daily.Sum(x => x.RegionsColored), activeSeconds = daily.Sum(x => x.ActiveSeconds) } });
});

app.MapPost("/api/admin/categories", [Authorize(Policy = "AdminOnly")] async (Category r, ClaimsPrincipal p, AppDbContext db) => { r.Id = Guid.NewGuid(); r.CreatedByUserId = UserId(p); db.Categories.Add(r); await Audit(db,p,"Category",r.Id,"Create",r); await db.SaveChangesAsync(); return Results.Created($"/api/admin/categories/{r.Id}",r); });
app.MapPost("/api/admin/artworks", [Authorize(Policy = "AdminOnly")] async (Artwork r, ClaimsPrincipal p, AppDbContext db) => { r.Id=Guid.NewGuid(); r.CreatedByUserId=UserId(p); r.PublishedAtUtc=r.IsPublished?DateTime.UtcNow:null; db.Artworks.Add(r); await Audit(db,p,"Artwork",r.Id,"Create",r); await db.SaveChangesAsync(); return Results.Created($"/api/admin/artworks/{r.Id}",r); });
app.MapPut("/api/admin/artworks/{id:guid}", [Authorize(Policy = "AdminOnly")] async (Guid id, Artwork r, ClaimsPrincipal p, AppDbContext db) => { var e=await db.Artworks.FindAsync(id); if(e is null)return Results.NotFound(); e.Title=r.Title;e.Description=r.Description;e.CategoryId=r.CategoryId;e.CountryCode=r.CountryCode;e.Brand=r.Brand;e.LicenseType=r.LicenseType;e.LicenseReference=r.LicenseReference;e.ThumbnailUrl=r.ThumbnailUrl;e.AssetUrl=r.AssetUrl;e.Difficulty=r.Difficulty;e.RegionCount=r.RegionCount;e.IsPublished=r.IsPublished;e.PublishedAtUtc??=r.IsPublished?DateTime.UtcNow:null;e.UpdatedAtUtc=DateTime.UtcNow;e.UpdatedByUserId=UserId(p);await Audit(db,p,"Artwork",id,"Update",r);await db.SaveChangesAsync();return Results.Ok(e); });
app.MapGet("/api/admin/users", [Authorize(Policy = "AdminOnly")] async (AppDbContext db) => Results.Ok(await db.Users.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Id,x.Email,x.DisplayName,x.Role,x.IsActive,x.CreatedAtUtc,x.LastLoginAtUtc }).ToListAsync()));
app.MapGet("/api/admin/audit", [Authorize(Policy = "AdminOnly")] async (AppDbContext db) => Results.Ok(await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(500).ToListAsync()));
app.MapGet("/api/admin/metrics", [Authorize(Policy = "AdminOnly")] async (AppDbContext db) => Results.Ok(new { users=await db.Users.CountAsync(),publishedArtworks=await db.Artworks.CountAsync(x=>x.IsPublished),completions=await db.UserArtworkProgress.CountAsync(x=>x.CompletedAtUtc!=null),activityEvents=await db.UserActivityHistory.CountAsync() }));
app.Run();

static Guid UserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
static Task Audit(AppDbContext db, ClaimsPrincipal p, string entity, Guid id, string action, object changes) => db.AuditLogs.AddAsync(new AuditLog { UserId=UserId(p),EntityName=entity,EntityId=id.ToString(),Action=action,ChangesJson=JsonSerializer.Serialize(changes) }).AsTask();
static async Task UpsertMetric(AppDbContext db, Guid userId, int sessions=0,int opened=0,int completed=0,int regions=0) { var date=DateOnly.FromDateTime(DateTime.UtcNow);var m=await db.UserMetricsDaily.SingleOrDefaultAsync(x=>x.UserId==userId&&x.MetricDate==date);if(m is null){m=new UserMetricDaily{UserId=userId,MetricDate=date};db.UserMetricsDaily.Add(m);}m.Sessions+=sessions;m.ArtworksOpened+=opened;m.ArtworksCompleted+=completed;m.RegionsColored+=regions; }

public sealed record RegisterRequest(string Email,string Password,string DisplayName);
public sealed record LoginRequest(string Email,string Password);
public sealed record ProgressRequest(decimal CompletionPercent,List<int> CompletedRegionIds,bool IsFavorite);
