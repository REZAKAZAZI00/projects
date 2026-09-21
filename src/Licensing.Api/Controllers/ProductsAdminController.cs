using Licensing.Application.Models;
using Licensing.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Roles = "Admin")]
public class ProductsAdminController : ControllerBase
{
    private readonly CatalogAdminService _catalogAdmin;
    private readonly CatalogService _catalog;

    public ProductsAdminController(CatalogAdminService catalogAdmin, CatalogService catalog)
    {
        _catalogAdmin = catalogAdmin;
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _catalog.GetProductsAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken) =>
        Ok(await _catalogAdmin.CreateProductAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await _catalogAdmin.UpdateProductAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _catalogAdmin.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{productId:guid}/features")]
    public async Task<IActionResult> Features(Guid productId, CancellationToken cancellationToken) =>
        Ok(await _catalog.GetProductFeaturesAsync(productId, cancellationToken));

    [HttpPost("{productId:guid}/features")]
    public async Task<IActionResult> CreateFeature(Guid productId, [FromBody] CreateProductFeatureRequest request, CancellationToken cancellationToken) =>
        Ok(await _catalogAdmin.CreateFeatureAsync(productId, request, cancellationToken));

    [HttpPut("features/{featureId:guid}")]
    public async Task<IActionResult> UpdateFeature(Guid featureId, [FromBody] UpdateProductFeatureRequest request, CancellationToken cancellationToken)
    {
        await _catalogAdmin.UpdateFeatureAsync(featureId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("features/{featureId:guid}")]
    public async Task<IActionResult> DeleteFeature(Guid featureId, CancellationToken cancellationToken)
    {
        await _catalogAdmin.DeleteFeatureAsync(featureId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{productId:guid}/plans")]
    public async Task<IActionResult> Plans(Guid productId, CancellationToken cancellationToken) =>
        Ok(await _catalog.GetPlansAsync(productId, cancellationToken));

    [HttpPost("{productId:guid}/plans")]
    public async Task<IActionResult> CreatePlan(Guid productId, [FromBody] CreatePlanRequest request, CancellationToken cancellationToken) =>
        Ok(await _catalogAdmin.CreatePlanAsync(productId, request, cancellationToken));

    [HttpPut("plans/{planId:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdatePlanRequest request, CancellationToken cancellationToken)
    {
        await _catalogAdmin.UpdatePlanAsync(planId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("plans/{planId:guid}")]
    public async Task<IActionResult> DeletePlan(Guid planId, CancellationToken cancellationToken)
    {
        await _catalogAdmin.DeletePlanAsync(planId, cancellationToken);
        return NoContent();
    }

    [HttpPut("plans/{planId:guid}/features")]
    public async Task<IActionResult> SetPlanFeatures(Guid planId, [FromBody] SetPlanFeaturesRequest request, CancellationToken cancellationToken)
    {
        await _catalogAdmin.SetPlanFeaturesAsync(planId, request, cancellationToken);
        return NoContent();
    }
}
