using System.Security.Claims;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppColoreando.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class UserContentController(IUserContentService service) : ControllerBase
{
    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress(CancellationToken ct) => Ok(await service.GetProgressAsync(UserId(), ct));

    [HttpPut("artworks/{artworkId:guid}/progress")]
    public async Task<IActionResult> SaveProgress(Guid artworkId, SaveProgressRequest request, CancellationToken ct)
        => Ok(await service.SaveProgressAsync(UserId(), artworkId, request, ct));

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct) => Ok(await service.GetHistoryAsync(UserId(), ct));

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct) => Ok(await service.GetMetricsAsync(UserId(), ct));

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
