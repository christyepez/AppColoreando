using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AppColoreando.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(ICatalogService service) : ControllerBase
{
    [HttpGet("artworks")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ArtworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArtworks(CancellationToken ct) => Ok(await service.GetPublishedAsync(ct));
}
