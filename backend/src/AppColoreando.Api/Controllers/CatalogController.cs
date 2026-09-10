using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AppColoreando.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(ICatalogService service) : ControllerBase
{
    [HttpGet("artworks/{id:guid}")]
    public async Task<IActionResult> GetArtwork(Guid id, CancellationToken ct)
    {
        var artwork = await service.GetArtworkAsync(id, ct);
        return artwork is null ? NotFound() : Ok(artwork);
    }

    [HttpGet("artworks/{id:guid}/bundle")]
    public async Task<IActionResult> GetBundle(Guid id, CancellationToken ct)
    {
        var asset = await service.GetArtworkArtifactAsync(id, "bundle", ct);
        return asset is null ? NotFound() : File(asset.Content, asset.ContentType);
    }

    [HttpGet("artworks/{id:guid}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken ct)
    {
        var asset = await service.GetArtworkArtifactAsync(id, "thumbnail", ct);
        return asset is null ? NotFound() : File(asset.Content, asset.ContentType);
    }

    [HttpGet("artworks")]
    [ProducesResponseType(typeof(PageResult<ArtworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArtworks([FromQuery] string? search, [FromQuery] string? countryCode, [FromQuery] Guid? categoryId, [FromQuery] Guid? collectionId, [FromQuery] int? difficulty, [FromQuery] bool? licensedOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 24, CancellationToken ct = default)
        => Ok(await service.SearchAsync(new CatalogQuery(search, countryCode, categoryId, collectionId, difficulty, licensedOnly, page, pageSize), ct));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct) => Ok(await service.GetCategoriesAsync(ct));

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken ct) => Ok(await service.GetCountriesAsync(ct));

    [HttpGet("collections")]
    public async Task<IActionResult> GetCollections(CancellationToken ct) => Ok(await service.GetCollectionsAsync(ct));
}
