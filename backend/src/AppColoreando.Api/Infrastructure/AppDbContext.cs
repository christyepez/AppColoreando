using AppColoreando.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppColoreando.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Artwork> Artworks => Set<Artwork>();
    public DbSet<UserArtworkProgress> UserArtworkProgress => Set<UserArtworkProgress>();
    public DbSet<UserActivityHistory> UserActivityHistory => Set<UserActivityHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserMetricDaily> UserMetricsDaily => Set<UserMetricDaily>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<UserArtworkProgress>().HasIndex(x => new { x.UserId, x.ArtworkId }).IsUnique();
        modelBuilder.Entity<UserMetricDaily>().HasIndex(x => new { x.UserId, x.MetricDate }).IsUnique();
        modelBuilder.Entity<Artwork>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
