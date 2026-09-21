using Licensing.Application.Models;
using Licensing.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly AuditQueryService _auditQueryService;

    public AuditController(AuditQueryService auditQueryService) => _auditQueryService = auditQueryService;

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] AuditQuery query, CancellationToken cancellationToken) =>
        Ok(await _auditQueryService.GetAuditLogsAsync(query, cancellationToken));
}
