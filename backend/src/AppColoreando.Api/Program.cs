using System.Text;
using AppColoreando.Application;
using AppColoreando.Infrastructure;
using AppColoreando.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");
if (jwtKey.Length < 32) throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AppColoreando";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AppColoreando.MobileAdmin";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("api", limiter =>
{
    limiter.PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 120);
    limiter.Window = TimeSpan.FromMinutes(1);
}));

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
builder.Services.AddAuthorizationBuilder().AddPolicy("ContentWrite", policy => policy.RequireRole("Admin", "ContentManager"));
builder.Services.AddAuthorizationBuilder().AddPolicy("AnalyticsRead", policy => policy.RequireRole("Admin", "Analyst"));
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
app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("X-Correlation-Id", context.TraceIdentifier);
    await next();
});
app.UseCors();
app.UseHttpMetrics();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics("/metrics");
await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
