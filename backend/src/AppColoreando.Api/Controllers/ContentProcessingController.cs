using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppColoreando.Api.Controllers;

[ApiController]
[Authorize(Policy = "ContentWrite")]
[Route("api/admin/content-processing")]
public sealed class ContentProcessingController(IArtworkBundleProcessor processor) : ControllerBase
{
    [HttpPost("validate-bundle")]
    [ProducesResponseType(typeof(BundleValidationResult), StatusCodes.Status200OK)]
    public IActionResult ValidateBundle([FromBody] JsonElementRequest request) => Ok(processor.Validate(request.Json));
}

public sealed record JsonElementRequest(string Json);
