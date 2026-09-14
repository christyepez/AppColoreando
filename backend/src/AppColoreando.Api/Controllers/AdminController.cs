using System.Security.Claims;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppColoreando.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(IAdminService service) : ControllerBase
{
    [Authorize(Policy = "ContentWrite")]
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await service.CreateCategoryAsync(UserId(), request, ct);
        return Created($"/api/admin/categories/{result.Id}", result);
    }

    [Authorize(Policy = "ContentWrite")]
    [HttpPost("countries")]
    public async Task<IActionResult> UpsertCountry(UpsertCountryRequest request, CancellationToken ct) => Ok(await service.UpsertCountryAsync(UserId(), request, ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand(UpsertBrandRequest request, CancellationToken ct) => Ok(await service.UpsertBrandAsync(UserId(), null, request, ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpPut("brands/{id:guid}")]
    public async Task<IActionResult> UpdateBrand(Guid id, UpsertBrandRequest request, CancellationToken ct) => Ok(await service.UpsertBrandAsync(UserId(), id, request, ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands(CancellationToken ct) => Ok(await service.GetBrandsAsync(ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpPost("licenses")]
    public async Task<IActionResult> CreateLicense(UpsertLicenseAgreementRequest request, CancellationToken ct) => Ok(await service.UpsertLicenseAsync(UserId(), null, request, ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpPut("licenses/{id:guid}")]
    public async Task<IActionResult> UpdateLicense(Guid id, UpsertLicenseAgreementRequest request, CancellationToken ct) => Ok(await service.UpsertLicenseAsync(UserId(), id, request, ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpGet("licenses")]
    public async Task<IActionResult> GetLicenses(CancellationToken ct) => Ok(await service.GetLicensesAsync(ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpPost("collections")]
    public async Task<IActionResult> CreateCollection(UpsertCollectionRequest request, CancellationToken ct) => Ok(await service.UpsertCollectionAsync(UserId(), null, request, ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpPut("collections/{id:guid}")]
    public async Task<IActionResult> UpdateCollection(Guid id, UpsertCollectionRequest request, CancellationToken ct) => Ok(await service.UpsertCollectionAsync(UserId(), id, request, ct));

    [Authorize(Policy = "ContentWrite")]
    [HttpPost("artworks")]
    public async Task<IActionResult> CreateArtwork(UpsertArtworkRequest request, CancellationToken ct)
    {
        var result = await service.CreateArtworkAsync(UserId(), request, ct);
        return Created($"/api/admin/artworks/{result.Id}", result);
    }

    [Authorize(Policy = "ContentWrite")]
    [HttpPut("artworks/{id:guid}")]
    public async Task<IActionResult> UpdateArtwork(Guid id, UpsertArtworkRequest request, CancellationToken ct)
    {
        var result = await service.UpdateArtworkAsync(UserId(), id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, AdminUpdateUserRequest request, CancellationToken ct)
    {
        var result = await service.UpdateUserAsync(UserId(), id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct) => Ok(await service.GetUsersAsync(ct));

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit(CancellationToken ct) => Ok(await service.GetAuditAsync(ct));

    [Authorize(Policy = "AnalyticsRead")]
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct) => Ok(await service.GetMetricsAsync(ct));

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
