using Licensing.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/v1/catalog")]
[Authorize(Roles = "Admin")]
public class CatalogController : ControllerBase
{
    private readonly CatalogService _catalogService;

    public CatalogController(CatalogService catalogService) => _catalogService = catalogService;

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken) =>
        Ok(await _catalogService.GetProductsAsync(cancellationToken));

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans([FromQuery] Guid? productId, CancellationToken cancellationToken) =>
        Ok(await _catalogService.GetPlansAsync(productId, cancellationToken));

    [HttpGet("products/{productId:guid}/features")]
    public async Task<IActionResult> GetProductFeatures(Guid productId, CancellationToken cancellationToken) =>
        Ok(await _catalogService.GetProductFeaturesAsync(productId, cancellationToken));
}
