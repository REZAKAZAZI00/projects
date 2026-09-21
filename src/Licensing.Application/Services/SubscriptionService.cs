using Licensing.Application.Abstractions;
using Licensing.Application.Common;
using Licensing.Application.Models;
using Licensing.Domain.Entities;
using Licensing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Application.Services;

public class SubscriptionService
{
    private readonly ILicensingDbContext _db;
    private readonly IAuditService _audit;
    private readonly IClock _clock;
    private readonly LicenseService _licenseService;
    private readonly LicenseLifecycleService _lifecycle;

    public SubscriptionService(
        ILicensingDbContext db,
        IAuditService audit,
        IClock clock,
        LicenseService licenseService,
        LicenseLifecycleService lifecycle)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
        _licenseService = licenseService;
        _lifecycle = lifecycle;
    }

    public async Task<SubscriptionDetailDto> CreateAsync(CreateSubscriptionRequest request, string actor, string? ip, CancellationToken cancellationToken = default)
    {
        await ValidateSubscriptionRequestAsync(request, cancellationToken);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            PlanId = request.PlanId,
            StartDateUtc = request.StartDateUtc,
            ExpirationDateUtc = request.ExpirationDateUtc,
            Status = request.Status,
            MetadataJson = request.MetadataJson,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.Subscriptions.Add(subscription);
        AddStatusHistory(subscription, SubscriptionStatus.Trial, subscription.Status, "Subscription created", actor);
        await _audit.LogAsync(AuditActionType.SubscriptionCreated, null, subscription.CustomerId, actor, ip,
            new { subscription.Id, subscription.PlanId }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (request.CreateLicense)
        {
            var plan = await _db.Plans.AsNoTracking().FirstAsync(p => p.Id == request.PlanId, cancellationToken);
            var maxActivations = request.MaxActivations ?? plan.DefaultMaxActivations;
            var licenseResult = await _licenseService.CreateLicenseAsync(
                new CreateLicenseRequest(
                    request.CustomerId,
                    request.ProductId,
                    request.PlanId,
                    request.ExpirationDateUtc,
                    maxActivations,
                    subscription.Id,
                    request.StartDateUtc,
                    ActivateImmediately: request.Status == SubscriptionStatus.Active,
                    AutoRenewal: false),
                actor,
                ip,
                cancellationToken);

            subscription.CurrentLicenseId = licenseResult.LicenseId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetAsync(subscription.Id, cancellationToken))!;
    }

    public async Task<SubscriptionDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureSubscriptionStatusesAsync(cancellationToken);

        var subscription = await _db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Product)
            .Include(s => s.Plan)
            .Include(s => s.Renewals)
            .Include(s => s.StatusHistory)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        return subscription is null ? null : MapDetail(subscription);
    }

    public async Task<PagedResult<SubscriptionListItemDto>> ListAsync(SubscriptionQuery query, CancellationToken cancellationToken = default)
    {
        await EnsureSubscriptionStatusesAsync(cancellationToken);

        var q = _db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Product)
            .Include(s => s.Plan)
            .Where(s => !s.IsDeleted);

        if (query.CustomerId.HasValue)
            q = q.Where(s => s.CustomerId == query.CustomerId.Value);
        if (query.ProductId.HasValue)
            q = q.Where(s => s.ProductId == query.ProductId.Value);
        if (query.Status.HasValue)
            q = q.Where(s => s.Status == query.Status.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(s => s.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SubscriptionListItemDto>(
            items.Select(MapList).ToList(),
            total,
            query.Page,
            query.PageSize);
    }

    public async Task RenewAsync(Guid id, RenewSubscriptionRequest request, string actor, string? ip, CancellationToken cancellationToken = default)
    {
        if (request.NewExpirationDateUtc <= _clock.UtcNow)
            throw new ValidationException("New expiration must be in the future (UTC).");

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Subscription not found.");

        if (subscription.Status == SubscriptionStatus.Cancelled)
            throw new ConflictException("Cancelled subscriptions cannot be renewed.");

        var previous = subscription.ExpirationDateUtc;
        var previousStatus = subscription.Status;
        subscription.ExpirationDateUtc = request.NewExpirationDateUtc;
        subscription.Status = SubscriptionStatus.Active;
        subscription.UpdatedAtUtc = _clock.UtcNow;
        AddStatusHistory(subscription, previousStatus, subscription.Status, "Subscription renewed", actor);

        _db.SubscriptionRenewals.Add(new SubscriptionRenewal
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            PreviousExpirationUtc = previous,
            NewExpirationUtc = request.NewExpirationDateUtc,
            RenewedAtUtc = _clock.UtcNow,
            RenewedBy = actor,
            Notes = request.Notes
        });

        await _audit.LogAsync(AuditActionType.SubscriptionRenewed, subscription.CurrentLicenseId, subscription.CustomerId, actor, ip,
            new { subscription.Id, previous, request.NewExpirationDateUtc }, cancellationToken: cancellationToken);

        if (request.SyncLicenseExpiration && subscription.CurrentLicenseId.HasValue)
        {
            await _lifecycle.RenewAsync(
                subscription.CurrentLicenseId.Value,
                request.NewExpirationDateUtc,
                actor,
                request.Notes,
                ip,
                syncLinkedSubscription: false,
                cancellationToken);
        }
        else
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CancelAsync(Guid id, string actor, string? reason, string? ip, CancellationToken cancellationToken = default)
    {
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Subscription not found.");

        var previousStatus = subscription.Status;
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.UpdatedAtUtc = _clock.UtcNow;
        AddStatusHistory(subscription, previousStatus, subscription.Status, reason ?? "Subscription cancelled", actor);

        if (subscription.CurrentLicenseId.HasValue)
            await _lifecycle.RevokeAsync(subscription.CurrentLicenseId.Value, actor, reason ?? "Subscription cancelled", ip, cancellationToken);
        else
            await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(AuditActionType.SubscriptionCancelled, subscription.CurrentLicenseId,
            subscription.CustomerId, actor, ip,
            new { subscription.Id, reason }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetGracePeriodAsync(Guid id, int graceDays, string actor, string? ip, CancellationToken cancellationToken = default)
    {
        if (graceDays < 1)
            throw new ValidationException("Grace days must be at least 1.");

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Subscription not found.");

        if (subscription.Status == SubscriptionStatus.Cancelled)
            throw new ConflictException("Cancelled subscriptions cannot enter a grace period.");

        var previousStatus = subscription.Status;
        subscription.Status = SubscriptionStatus.GracePeriod;
        var graceExpiration = _clock.UtcNow.AddDays(graceDays);
        subscription.ExpirationDateUtc = graceExpiration;
        subscription.UpdatedAtUtc = _clock.UtcNow;
        AddStatusHistory(subscription, previousStatus, subscription.Status, "Subscription grace period started", actor);

        if (subscription.CurrentLicenseId.HasValue)
        {
            await _lifecycle.RenewAsync(
                subscription.CurrentLicenseId.Value,
                graceExpiration,
                actor,
                "Subscription grace period started",
                ip,
                syncLinkedSubscription: false,
                cancellationToken);
        }
        else
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _audit.LogAsync(AuditActionType.SubscriptionGracePeriodStarted, subscription.CurrentLicenseId,
            subscription.CustomerId, actor, ip,
            new { subscription.Id, graceDays, graceExpiration }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureSubscriptionStatusesAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var expired = await _db.Subscriptions
            .Where(s => !s.IsDeleted &&
                        s.Status != SubscriptionStatus.Cancelled &&
                        s.Status != SubscriptionStatus.Expired &&
                        s.ExpirationDateUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (var sub in expired)
        {
            var previousStatus = sub.Status;
            sub.Status = SubscriptionStatus.Expired;
            sub.UpdatedAtUtc = now;
            AddStatusHistory(sub, previousStatus, sub.Status, "Subscription expired", "System");
            if (sub.CurrentLicenseId.HasValue)
            {
                var license = await _db.Licenses.FirstOrDefaultAsync(l => l.Id == sub.CurrentLicenseId.Value, cancellationToken);
                if (license is not null && license.Status is LicenseStatus.Active or LicenseStatus.Suspended or LicenseStatus.Pending)
                    await _lifecycle.ChangeStatusAsync(license, LicenseStatus.Expired, "System", "Subscription expired", cancellationToken);
            }
        }

        if (expired.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionStatusHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Subscriptions.AnyAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (!exists)
            throw new NotFoundException("Subscription not found.");

        return await _db.SubscriptionStatusHistories.AsNoTracking()
            .Where(h => h.SubscriptionId == id)
            .OrderByDescending(h => h.ChangedAtUtc)
            .Select(h => new SubscriptionStatusHistoryDto(
                h.Id,
                h.FromStatus.ToString(),
                h.ToStatus.ToString(),
                h.Reason,
                h.ChangedBy,
                h.ChangedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateSubscriptionRequestAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (request.ExpirationDateUtc <= request.StartDateUtc)
            throw new ValidationException("Expiration must be after start date.");

        var customerOk = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId && !c.IsDeleted, cancellationToken);
        if (!customerOk)
            throw new NotFoundException("Customer not found.");

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId && p.ProductId == request.ProductId && p.IsActive, cancellationToken);
        if (plan is null)
            throw new NotFoundException("Plan not found for product.");
    }

    private SubscriptionListItemDto MapList(Subscription s) => new(
        s.Id,
        s.CustomerId,
        s.Customer.Name,
        s.ProductId,
        s.Product.Name,
        s.PlanId,
        s.Plan.Name,
        s.StartDateUtc,
        s.ExpirationDateUtc,
        s.Status.ToString(),
        s.CurrentLicenseId,
        s.ExpirationDateUtc - _clock.UtcNow);

    private SubscriptionDetailDto MapDetail(Subscription s) => new(
        s.Id,
        s.CustomerId,
        s.Customer.Name,
        s.ProductId,
        s.Product.Name,
        s.PlanId,
        s.Plan.Name,
        s.StartDateUtc,
        s.ExpirationDateUtc,
        s.Status.ToString(),
        s.CurrentLicenseId,
        s.MetadataJson,
        s.ExpirationDateUtc - _clock.UtcNow,
        s.Renewals.OrderByDescending(r => r.RenewedAtUtc)
            .Select(r => new SubscriptionRenewalDto(r.Id, r.PreviousExpirationUtc, r.NewExpirationUtc, r.RenewedAtUtc, r.RenewedBy, r.Notes))
            .ToList(),
        s.StatusHistory.OrderByDescending(h => h.ChangedAtUtc)
            .Select(h => new SubscriptionStatusHistoryDto(h.Id, h.FromStatus.ToString(), h.ToStatus.ToString(), h.Reason, h.ChangedBy, h.ChangedAtUtc))
            .ToList());

    private void AddStatusHistory(Subscription subscription, SubscriptionStatus fromStatus, SubscriptionStatus toStatus, string reason, string changedBy)
    {
        _db.SubscriptionStatusHistories.Add(new SubscriptionStatusHistory
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Reason = reason,
            ChangedBy = changedBy,
            ChangedAtUtc = _clock.UtcNow
        });
    }
}
