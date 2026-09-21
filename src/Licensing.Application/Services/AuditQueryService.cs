using Licensing.Application.Abstractions;
using Licensing.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Application.Services;

public class AuditQueryService
{
    private readonly ILicensingDbContext _db;

    public AuditQueryService(ILicensingDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.LicenseAuditLogs.AsNoTracking().AsQueryable();

        if (query.LicenseId.HasValue)
            q = q.Where(a => a.LicenseId == query.LicenseId.Value);

        if (query.CustomerId.HasValue)
            q = q.Where(a => a.CustomerId == query.CustomerId.Value);

        if (query.SuspiciousOnly == true)
            q = q.Where(a => a.IsSuspicious);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.ActionType.ToString(),
                a.LicenseId,
                a.CustomerId,
                a.Actor,
                a.IpAddress,
                a.DetailsJson,
                a.IsSuspicious,
                a.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, total, query.Page, query.PageSize);
    }
}
