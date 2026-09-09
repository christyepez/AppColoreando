using AppColoreando.Application.Abstractions;
using AppColoreando.Domain.Entities;
using AppColoreando.Infrastructure.ContentGeneration;
using AppColoreando.Infrastructure.Persistence;
using AppColoreando.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppColoreando.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IArtworkRepository, ArtworkRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ILicenseRepository, LicenseRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IUserProgressRepository, UserProgressRepository>();
        services.AddScoped<ISyncOperationRepository, SyncOperationRepository>();
        services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
        services.AddScoped<IUserActivityRepository, UserActivityRepository>();
        services.AddScoped<IUserMetricRepository, UserMetricRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<ISourceAssetRepository, SourceAssetRepository>();
        services.AddScoped<IStylePresetRepository, StylePresetRepository>();
        services.AddScoped<IGenerationJobRepository, GenerationJobRepository>();
        services.AddSingleton<ISourceAssetStorage, FileSystemSourceAssetStorage>();
        services.AddSingleton<IGenerationArtifactReader, FileSystemGenerationArtifactReader>();
        services.AddSingleton<IGenerationAdjustmentStore, FileSystemGenerationAdjustmentStore>();
        services.AddSingleton<IGenerationJobQueue, RabbitMqGenerationJobQueue>();
        services.AddHttpClient<IVisualProcessorClient, VisualProcessorClient>(client =>
        {
            var baseUrl = configuration["VisualProcessor:BaseUrl"] ?? "http://localhost:8090/";
            client.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        return services;
    }
}
