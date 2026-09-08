using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AppColoreando.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IUserContentService, UserContentService>();
        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}
