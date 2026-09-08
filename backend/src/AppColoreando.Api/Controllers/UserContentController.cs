using System.Security.Claims;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppColoreando.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class UserContentController(IUserContentService service, IUserAccountService accounts, IAuthService auth) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct) => Ok(await accounts.GetProfileAsync(UserId(), ct));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken ct) => Ok(await accounts.UpdateProfileAsync(UserId(), request, ct));

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken ct) => Ok(await accounts.GetSessionsAsync(UserId(), ct));

    [HttpPost("sessions/revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken ct)
    {
        await auth.RevokeAllAsync(UserId(), ct);
        return NoContent();
    }

    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress(CancellationToken ct) => Ok(await service.GetProgressAsync(UserId(), ct));

    [HttpPut("artworks/{artworkId:guid}/progress")]
    public async Task<IActionResult> SaveProgress(Guid artworkId, SaveProgressRequest request, CancellationToken ct)
        => Ok(await service.SaveProgressAsync(UserId(), artworkId, request, ct));

    [HttpGet("library")]
    public async Task<IActionResult> GetLibrary(CancellationToken ct) => Ok(await service.GetLibraryAsync(UserId(), ct));

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct) => Ok(await service.GetHistoryAsync(UserId(), ct));

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct) => Ok(await service.GetMetricsAsync(UserId(), ct));

    [HttpGet("achievements")]
    public async Task<IActionResult> GetAchievements(CancellationToken ct) => Ok(await service.GetAchievementsAsync(UserId(), ct));

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
