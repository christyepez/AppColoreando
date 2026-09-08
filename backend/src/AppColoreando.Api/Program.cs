using System.Text;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Services;
using AppColoreando.Domain.Entities;
using AppColoreando.Infrastructure.Persistence;
using AppColoreando.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");
if (jwtKey.Length < 32) throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AppColoreando";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AppColoreando.MobileAdmin";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArtworkRepository, ArtworkRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUserProgressRepository, UserProgressRepository>();
builder.Services.AddScoped<IUserActivityRepository, UserActivityRepository>();
builder.Services.AddScoped<IUserMetricRepository, UserMetricRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IUserContentService, UserContentService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorizationBuilder().AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = error switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest, "Validation error"),
        InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
    };
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = app.Environment.IsDevelopment() ? error?.Message : null
    });
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program;
