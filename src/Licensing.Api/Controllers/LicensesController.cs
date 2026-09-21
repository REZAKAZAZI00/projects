using Licensing.Api.Extensions;
using Licensing.Application.Models;
using Licensing.Application.Services;
using Licensing.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/v1/licenses")]
[Authorize(Roles = "Admin")]
public class LicensesController : ControllerBase
{
    private readonly LicenseService _licenseService;
    private readonly LicenseLifecycleService _lifecycleService;

    public LicensesController(LicenseService licenseService, LicenseLifecycleService lifecycleService)
    {
        _licenseService = licenseService;
        _lifecycleService = lifecycleService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLicenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _licenseService.CreateLicenseAsync(request, User.GetActor(), HttpContext.GetClientIp(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.LicenseId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var license = await _licenseService.GetLicenseAsync(id, cancellationToken);
        return license is null ? NotFound(new { error = "License not found." }) : Ok(license);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] LicenseQuery query, CancellationToken cancellationToken)
    {
        var result = await _licenseService.GetLicensesAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLicenseRequest request, CancellationToken cancellationToken)
    {
        await _licenseService.UpdateLicenseAsync(id, request, User.GetActor(), HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/renew")]
    public async Task<IActionResult> Renew(Guid id, [FromBody] RenewLicenseRequest request, CancellationToken cancellationToken)
    {
        await _lifecycleService.RenewAsync(id, request.NewExpirationDateUtc, User.GetActor(), request.Notes, HttpContext.GetClientIp(), cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] StatusChangeRequest request, CancellationToken cancellationToken)
    {
        await _lifecycleService.SuspendAsync(id, User.GetActor(), request.Reason, HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, [FromBody] StatusChangeRequest request, CancellationToken cancellationToken)
    {
        await _lifecycleService.ResumeAsync(id, User.GetActor(), request.Reason, HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] StatusChangeRequest request, CancellationToken cancellationToken)
    {
        await _lifecycleService.RevokeAsync(id, User.GetActor(), request.Reason, HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/activations")]
    public async Task<IActionResult> GetActivations(Guid id, CancellationToken cancellationToken)
    {
        var items = await _licenseService.GetActivationsAsync(id, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var items = await _licenseService.GetHistoryAsync(id, cancellationToken);
        return Ok(items);
    }

    [HttpGet("customers/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        var items = await _licenseService.GetCustomerLicensesAsync(customerId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("lookup/by-key")]
    public async Task<IActionResult> LookupByKey([FromQuery] string licenseKey, CancellationToken cancellationToken)
    {
        var license = await _licenseService.GetLicenseByKeyAsync(licenseKey, cancellationToken);
        return license is null ? NotFound(new { error = "License not found." }) : Ok(license);
    }

    [HttpPost("{licenseId:guid}/activations/{activationId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateActivation(Guid licenseId, Guid activationId, CancellationToken cancellationToken)
    {
        await _licenseService.AdminDeactivateActivationAsync(licenseId, activationId, User.GetActor(), HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    public record RenewLicenseRequest(DateTime NewExpirationDateUtc, string? Notes);
    public record StatusChangeRequest(string? Reason);
}
