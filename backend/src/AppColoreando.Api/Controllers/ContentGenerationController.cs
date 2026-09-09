using System.Security.Claims;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppColoreando.Api.Controllers;

[ApiController]
[Authorize(Policy = "ContentWrite")]
[Route("api/admin/content-generation")]
public sealed class ContentGenerationController(IContentGenerationService service) : ControllerBase
{
    [HttpPost("source-assets")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> UploadSourceAsset([FromForm] IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await service.UploadSourceAssetAsync(
            UserId(), file.FileName, file.ContentType, file.Length, stream, ct);
        return Created($"/api/admin/content-generation/source-assets/{result.Id}", result);
    }

    [HttpGet("source-assets")]
    public async Task<IActionResult> GetSourceAssets(CancellationToken ct, [FromQuery] int take = 50) =>
        Ok(await service.GetSourceAssetsAsync(take, ct));
    [HttpGet("style-presets")]
    public async Task<IActionResult> GetStylePresets(CancellationToken ct) =>
        Ok(await service.GetStylePresetsAsync(ct));

    [HttpPost("jobs")]
    public async Task<IActionResult> CreateJob(CreateGenerationJobRequest request, CancellationToken ct)
    {
        var result = await service.CreateGenerationJobAsync(UserId(), request, ct);
        return Accepted($"/api/admin/content-generation/jobs/{result.Id}", result);
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs(CancellationToken ct, [FromQuery] int take = 50) =>
        Ok(await service.GetGenerationJobsAsync(take, ct));

    [HttpGet("jobs/{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken ct)
    {
        var result = await service.GetGenerationJobAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("jobs/{id:guid}/artifacts/{kind}")]
    public async Task<IActionResult> GetArtifact(Guid id, string kind, CancellationToken ct)
    {
        var artifact = await service.GetGenerationArtifactAsync(id, kind, ct);
        return artifact is null ? NotFound() : File(artifact.Content, artifact.ContentType);
    }

    [HttpGet("jobs/{id:guid}/adjustments")]
    public async Task<IActionResult> GetAdjustments(Guid id, CancellationToken ct) =>
        Ok(await service.GetGenerationAdjustmentsAsync(id, ct));

    [HttpPut("jobs/{id:guid}/adjustments/{regionId:int}")]
    public async Task<IActionResult> UpsertAdjustment(Guid id, int regionId, RegionAdjustmentRequest request, CancellationToken ct) =>
        Ok(await service.UpsertGenerationAdjustmentAsync(UserId(), id, regionId, request, ct));

    [HttpDelete("jobs/{id:guid}/adjustments/{regionId:int}")]
    public async Task<IActionResult> DeleteAdjustment(Guid id, int regionId, CancellationToken ct)
    {
        await service.DeleteGenerationAdjustmentAsync(UserId(), id, regionId, ct);
        return NoContent();
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
