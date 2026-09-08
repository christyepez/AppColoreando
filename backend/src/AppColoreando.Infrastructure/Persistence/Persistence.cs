using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppColoreando.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Artwork> Artworks => Set<Artwork>();
    public DbSet<UserArtworkProgress> UserArtworkProgress => Set<UserArtworkProgress>();
    public DbSet<UserActivityHistory> UserActivityHistory => Set<UserActivityHistory>();
    public DbSet<UserMetricDaily> UserMetricsDaily => Set<UserMetricDaily>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Artwork>().HasIndex(x => new { x.CategoryId, x.IsPublished });
        b.Entity<UserArtworkProgress>().HasIndex(x => new { x.UserId, x.ArtworkId }).IsUnique();
        b.Entity<UserMetricDaily>().HasIndex(x => new { x.UserId, x.MetricDate }).IsUnique();
        b.Entity<Artwork>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<UserArtworkProgress>().Property(x => x.CompletionPercent).HasPrecision(5,2);
    }
}

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)=>db.Users.AnyAsync(x=>x.Email==email,ct);
    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct)=>db.Users.SingleOrDefaultAsync(x=>x.Email==email,ct);
    public async Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken ct)=>await db.Users.AsNoTracking().OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new UserDto(x.Id,x.Email,x.DisplayName,x.Role,x.IsActive,x.CreatedAtUtc,x.LastLoginAtUtc)).ToArrayAsync(ct);
    public async Task AddAsync(AppUser user,CancellationToken ct)=>await db.Users.AddAsync(user,ct);
}

public sealed class ArtworkRepository(AppDbContext db) : IArtworkRepository
{
    public Task<bool> PublishedExistsAsync(Guid id,CancellationToken ct)=>db.Artworks.AnyAsync(x=>x.Id==id&&x.IsPublished,ct);
    public Task<Artwork?> GetAsync(Guid id,CancellationToken ct)=>db.Artworks.SingleOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<IReadOnlyCollection<ArtworkDto>> ListPublishedAsync(CancellationToken ct)=>await db.Artworks.AsNoTracking().Where(x=>x.IsPublished).OrderByDescending(x=>x.PublishedAtUtc).Select(x=>new ArtworkDto(x.Id,x.Title,x.Description,x.CategoryId,x.CountryCode,x.Brand,x.LicenseType,x.LicenseReference,x.ThumbnailUrl,x.AssetUrl,x.Difficulty,x.RegionCount,x.IsPublished,x.PublishedAtUtc)).ToArrayAsync(ct);
    public async Task AddAsync(Artwork artwork,CancellationToken ct)=>await db.Artworks.AddAsync(artwork,ct);
    public Task<int> CountPublishedAsync(CancellationToken ct)=>db.Artworks.CountAsync(x=>x.IsPublished,ct);
}

public sealed class CategoryRepository(AppDbContext db):ICategoryRepository { public async Task AddAsync(Category category,CancellationToken ct)=>await db.Categories.AddAsync(category,ct); }

public sealed class UserProgressRepository(AppDbContext db):IUserProgressRepository
{
    public Task<UserArtworkProgress?> GetAsync(Guid userId,Guid artworkId,CancellationToken ct)=>db.UserArtworkProgress.SingleOrDefaultAsync(x=>x.UserId==userId&&x.ArtworkId==artworkId,ct);
    public async Task<IReadOnlyCollection<UserArtworkProgress>> ListAsync(Guid userId,CancellationToken ct)=>await db.UserArtworkProgress.AsNoTracking().Where(x=>x.UserId==userId).OrderByDescending(x=>x.LastOpenedAtUtc).ToArrayAsync(ct);
    public Task<int> CountCompletedAsync(CancellationToken ct)=>db.UserArtworkProgress.CountAsync(x=>x.CompletedAtUtc!=null,ct);
    public void Add(UserArtworkProgress progress)=>db.UserArtworkProgress.Add(progress);
}

public sealed class UserActivityRepository(AppDbContext db):IUserActivityRepository
{
    public void Add(UserActivityHistory activity)=>db.UserActivityHistory.Add(activity);
    public async Task<IReadOnlyCollection<UserActivityDto>> ListAsync(Guid userId,int take,CancellationToken ct)=>await db.UserActivityHistory.AsNoTracking().Where(x=>x.UserId==userId).OrderByDescending(x=>x.OccurredAtUtc).Take(take).Select(x=>new UserActivityDto(x.Id,x.ArtworkId,x.ActivityType,x.MetadataJson,x.OccurredAtUtc)).ToArrayAsync(ct);
    public Task<int> CountAsync(CancellationToken ct)=>db.UserActivityHistory.CountAsync(ct);
}

public sealed class UserMetricRepository(AppDbContext db):IUserMetricRepository
{
    public Task<UserMetricDaily?> GetAsync(Guid userId,DateOnly date,CancellationToken ct)=>db.UserMetricsDaily.SingleOrDefaultAsync(x=>x.UserId==userId&&x.MetricDate==date,ct);
    public void Add(UserMetricDaily metric)=>db.UserMetricsDaily.Add(metric);
    public async Task<IReadOnlyCollection<UserMetricDaily>> ListAsync(Guid userId,int days,CancellationToken ct)=>await db.UserMetricsDaily.AsNoTracking().Where(x=>x.UserId==userId).OrderByDescending(x=>x.MetricDate).Take(days).ToArrayAsync(ct);
}

public sealed class AuditRepository(AppDbContext db):IAuditRepository
{
    public void Add(AuditLog audit)=>db.AuditLogs.Add(audit);
    public async Task<IReadOnlyCollection<AuditLogDto>> ListAsync(int take,CancellationToken ct)=>await db.AuditLogs.AsNoTracking().OrderByDescending(x=>x.OccurredAtUtc).Take(take).Select(x=>new AuditLogDto(x.Id,x.UserId,x.EntityName,x.EntityId,x.Action,x.ChangesJson,x.OccurredAtUtc)).ToArrayAsync(ct);
}
