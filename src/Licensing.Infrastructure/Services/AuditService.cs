using System.Text.Json;
using Licensing.Application.Abstractions;
using Licensing.Domain.Entities;
using Licensing.Domain.Enums;

namespace Licensing.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ILicensingDbContext _db;
    private readonly IClock _clock;

    public AuditService(ILicensingDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public Task LogAsync(
        AuditActionType actionType,
        Guid? licenseId,
        Guid? customerId,
        string? actor,
        string? ipAddress,
        object? details = null,
        bool isSuspicious = false,
        CancellationToken cancellationToken = default)
    {
        var sanitizedDetails = SanitizeDetails(details);
        _db.LicenseAuditLogs.Add(new LicenseAuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = actionType,
            LicenseId = licenseId,
            CustomerId = customerId,
            Actor = actor,
            IpAddress = ipAddress,
            DetailsJson = sanitizedDetails,
            CreatedAtUtc = _clock.UtcNow,
            IsSuspicious = isSuspicious
        });

        return Task.CompletedTask;
    }

    private static string? SanitizeDetails(object? details)
    {
        if (details is null)
            return null;

        var json = JsonSerializer.Serialize(details);
        return json
            .Replace("licenseKey", "redacted", StringComparison.OrdinalIgnoreCase)
            .Replace("password", "redacted", StringComparison.OrdinalIgnoreCase);
    }
}
