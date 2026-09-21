using Licensing.Api.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using Licensing.Application.Models;
using Licensing.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/v1/client/licenses")]
public class ClientLicensesController : ControllerBase
{
    private readonly LicenseService _licenseService;

    public ClientLicensesController(LicenseService licenseService) => _licenseService = licenseService;

    [HttpPost("activate")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> Activate([FromBody] ActivateLicenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _licenseService.ActivateAsync(request, HttpContext.GetClientIp(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("deactivate")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateLicenseRequest request, CancellationToken cancellationToken)
    {
        await _licenseService.DeactivateAsync(request, HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    [HttpPost("validate")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> Validate([FromBody] ValidateLicenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _licenseService.ValidateAsync(request, HttpContext.GetClientIp(), cancellationToken);
        return Ok(result);
    }
}
