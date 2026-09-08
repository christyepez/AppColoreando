using System.Security.Claims;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppColoreando.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin")]
public sealed class AdminController(IAdminService service) : ControllerBase
{
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await service.CreateCategoryAsync(UserId(), request, ct);
        return Created($"/api/admin/categories/{result.Id}", result);
    }

    [HttpPost("artworks")]
    public async Task<IActionResult> CreateArtwork(CreateArtworkRequest request, CancellationToken ct)
    {
        var result = await service.CreateArtworkAsync(UserId(), request, ct);
        return Created($"/api/admin/artworks/{result.Id}", result);
    }

    [HttpPut("artworks/{id:guid}")]
    public async Task<IActionResult> UpdateArtwork(Guid id, UpdateArtworkRequest request, CancellationToken ct)
    {
        var result = await service.UpdateArtworkAsync(UserId(), id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct) => Ok(await service.GetUsersAsync(ct));

    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit(CancellationToken ct) => Ok(await service.GetAuditAsync(ct));

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct) => Ok(await service.GetMetricsAsync(ct));

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
