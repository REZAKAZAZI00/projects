using Licensing.Application.Abstractions;
using Licensing.Application.Common;
using Licensing.Domain.Entities;
using Licensing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Application.Services;

public class LicenseLifecycleService
{
    private readonly ILicensingDbContext _db;
    private readonly IAuditService _audit;
    private readonly IClock _clock;

    public LicenseLifecycleService(ILicensingDbContext db, IAuditService audit, IClock clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async Task EnsureExpiredLicensesUpdatedAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var licenses = await _db.Licenses
            .Where(l => !l.IsDeleted &&
                        (l.Status == LicenseStatus.Active || l.Status == LicenseStatus.Suspended || l.Status == LicenseStatus.Pending) &&
                        l.ExpirationDateUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (var license in licenses)
        {
            await ChangeStatusAsync(license, LicenseStatus.Expired, "System", "Automatic expiration", cancellationToken);
            await _audit.LogAsync(AuditActionType.LicenseExpired, license.Id, license.CustomerId, "System", null,
                new { license.ExpirationDateUtc }, cancellationToken: cancellationToken);
        }
    }

    public async Task ChangeStatusAsync(
        License license,
        LicenseStatus newStatus,
        string changedBy,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (license.Status == newStatus)
            return;

        if (license.Status == LicenseStatus.Revoked)
            throw new ConflictException("A revoked license cannot change status.");

        var history = new LicenseStatusHistory
        {
            Id = Guid.NewGuid(),
            LicenseId = license.Id,
            FromStatus = license.Status,
            ToStatus = newStatus,
            Reason = reason,
            ChangedBy = changedBy,
            ChangedAtUtc = _clock.UtcNow
        };

        license.Status = newStatus;
        license.UpdatedAtUtc = _clock.UtcNow;
        _db.LicenseStatusHistories.Add(history);
    }

    public async Task SuspendAsync(Guid licenseId, string actor, string? reason, string? ip, CancellationToken cancellationToken = default)
    {
        var license = await GetLicenseOrThrow(licenseId, cancellationToken);
        if (license.Status != LicenseStatus.Active && license.Status != LicenseStatus.Pending)
            throw new ConflictException("Only active or pending licenses can be suspended.");

        await ChangeStatusAsync(license, LicenseStatus.Suspended, actor, reason, cancellationToken);
        await _audit.LogAsync(AuditActionType.LicenseSuspended, license.Id, license.CustomerId, actor, ip,
            new { reason }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResumeAsync(Guid licenseId, string actor, string? reason, string? ip, CancellationToken cancellationToken = default)
    {
        var license = await GetLicenseOrThrow(licenseId, cancellationToken);
        if (license.Status != LicenseStatus.Suspended)
            throw new ConflictException("Only suspended licenses can be resumed.");

        if (license.ExpirationDateUtc <= _clock.UtcNow)
            throw new ConflictException("Cannot resume an expired license. Renew it first.");

        await ChangeStatusAsync(license, LicenseStatus.Active, actor, reason, cancellationToken);
        await _audit.LogAsync(AuditActionType.LicenseResumed, license.Id, license.CustomerId, actor, ip,
            new { reason }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(Guid licenseId, string actor, string? reason, string? ip, CancellationToken cancellationToken = default)
    {
        var license = await GetLicenseOrThrow(licenseId, cancellationToken);
        if (license.Status == LicenseStatus.Revoked)
            throw new ConflictException("License is already revoked.");

        await ChangeStatusAsync(license, LicenseStatus.Revoked, actor, reason, cancellationToken);

        var activeActivations = await _db.LicenseActivations
            .Where(a => a.LicenseId == licenseId && a.Status == ActivationStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var activation in activeActivations)
        {
            activation.Status = ActivationStatus.Deactivated;
            activation.DeactivatedAtUtc = _clock.UtcNow;
        }

        await _audit.LogAsync(AuditActionType.LicenseRevoked, license.Id, license.CustomerId, actor, ip,
            new { reason }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RenewAsync(
        Guid licenseId,
        DateTime newExpirationUtc,
        string actor,
        string? notes,
        string? ip,
        bool syncLinkedSubscription = true,
        CancellationToken cancellationToken = default)
    {
        if (newExpirationUtc <= _clock.UtcNow)
            throw new ValidationException("New expiration must be in the future (UTC).");

        var license = await GetLicenseOrThrow(licenseId, cancellationToken);
        if (license.Status == LicenseStatus.Revoked)
            throw new ConflictException("Revoked licenses cannot be renewed.");

        var previous = license.ExpirationDateUtc;
        license.ExpirationDateUtc = newExpirationUtc;
        license.UpdatedAtUtc = _clock.UtcNow;

        if (license.Status == LicenseStatus.Expired)
            await ChangeStatusAsync(license, LicenseStatus.Active, actor, "Renewed after expiration", cancellationToken);

        if (syncLinkedSubscription && license.SubscriptionId.HasValue)
        {
            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == license.SubscriptionId.Value, cancellationToken);
            if (subscription is not null)
            {
                var subPrevious = subscription.ExpirationDateUtc;
                subscription.ExpirationDateUtc = newExpirationUtc;
                subscription.Status = SubscriptionStatus.Active;
                subscription.UpdatedAtUtc = _clock.UtcNow;
                _db.SubscriptionRenewals.Add(new SubscriptionRenewal
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = subscription.Id,
                    PreviousExpirationUtc = subPrevious,
                    NewExpirationUtc = newExpirationUtc,
                    RenewedAtUtc = _clock.UtcNow,
                    RenewedBy = actor,
                    Notes = notes
                });
            }
        }

        await _audit.LogAsync(AuditActionType.LicenseRenewed, license.Id, license.CustomerId, actor, ip,
            new { previousExpirationUtc = previous, newExpirationUtc, notes }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<License> GetLicenseOrThrow(Guid licenseId, CancellationToken cancellationToken)
    {
        var license = await _db.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId && !l.IsDeleted, cancellationToken);
        if (license is null)
            throw new NotFoundException("License not found.");
        return license;
    }
}
