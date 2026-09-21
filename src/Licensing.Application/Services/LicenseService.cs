using Licensing.Application.Abstractions;
using Licensing.Application.Common;
using Licensing.Application.Models;
using Licensing.Domain.Entities;
using Licensing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Application.Services;

public class LicenseService
{
    private readonly ILicensingDbContext _db;
    private readonly ILicenseKeyService _keyService;
    private readonly IAuditService _audit;
    private readonly IClock _clock;
    private readonly LicenseLifecycleService _lifecycle;

    public LicenseService(
        ILicensingDbContext db,
        ILicenseKeyService keyService,
        IAuditService audit,
        IClock clock,
        LicenseLifecycleService lifecycle)
    {
        _db = db;
        _keyService = keyService;
        _audit = audit;
        _clock = clock;
        _lifecycle = lifecycle;
    }

    public async Task<CreateLicenseResult> CreateLicenseAsync(CreateLicenseRequest request, string actor, string? ip, CancellationToken cancellationToken = default)
    {
        await ValidateCreateRequestAsync(request, cancellationToken);

        var plainKey = _keyService.GenerateLicenseKey();
        var license = new License
        {
            Id = Guid.NewGuid(),
            LicenseKeyHash = _keyService.HashLicenseKey(plainKey),
            LicenseKeyPrefix = _keyService.GetKeyPrefix(plainKey),
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            PlanId = request.PlanId,
            SubscriptionId = request.SubscriptionId,
            Status = request.ActivateImmediately ? LicenseStatus.Active : LicenseStatus.Pending,
            ValidFromUtc = request.ValidFromUtc ?? _clock.UtcNow,
            ExpirationDateUtc = request.ExpirationDateUtc,
            MaxActivations = request.MaxActivations,
            AutoRenewal = request.AutoRenewal,
            Description = request.Description,
            CreatedBy = actor,
            CreatedAtUtc = _clock.UtcNow
        };

        await CopyPlanFeaturesAndLimitsAsync(license, request, cancellationToken);

        _db.Licenses.Add(license);
        _db.LicenseStatusHistories.Add(new LicenseStatusHistory
        {
            Id = Guid.NewGuid(),
            LicenseId = license.Id,
            FromStatus = LicenseStatus.Pending,
            ToStatus = license.Status,
            ChangedBy = actor,
            ChangedAtUtc = _clock.UtcNow,
            Reason = "License created"
        });

        if (license.SubscriptionId.HasValue)
        {
            var subscription = await _db.Subscriptions.FirstAsync(s => s.Id == license.SubscriptionId.Value, cancellationToken);
            subscription.CurrentLicenseId = license.Id;
            subscription.UpdatedAtUtc = _clock.UtcNow;
        }

        await _audit.LogAsync(AuditActionType.LicenseCreated, license.Id, license.CustomerId, actor, ip,
            new { license.ProductId, license.PlanId, license.MaxActivations }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateLicenseResult(license.Id, plainKey);
    }

    public async Task<LicenseDetailDto?> GetLicenseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var license = await QueryLicenseDetails()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        return license is null ? null : MapDetail(license);
    }

    public async Task<LicenseDetailDto?> GetLicenseByKeyAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        var license = await FindByKeyAsync(licenseKey, cancellationToken);
        if (license is null)
            return null;

        return await GetLicenseAsync(license.Id, cancellationToken);
    }

    public async Task AdminDeactivateActivationAsync(Guid licenseId, Guid activationId, string actor, string? ip, CancellationToken cancellationToken = default)
    {
        var license = await _db.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId && !l.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("License not found.");

        var activation = await _db.LicenseActivations
            .FirstOrDefaultAsync(a => a.Id == activationId && a.LicenseId == licenseId && a.Status == ActivationStatus.Active, cancellationToken)
            ?? throw new NotFoundException("Active activation not found.");

        activation.Status = ActivationStatus.Deactivated;
        activation.DeactivatedAtUtc = _clock.UtcNow;

        await _audit.LogAsync(AuditActionType.LicenseDeactivated, license.Id, license.CustomerId, actor, ip,
            new { activationId, activation.InstanceIdentifier, source = "admin" }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<LicenseListItemDto>> GetLicensesAsync(LicenseQuery query, CancellationToken cancellationToken = default)
    {
        await _lifecycle.EnsureExpiredLicensesUpdatedAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.LicenseKey))
        {
            var matched = await FindByKeyAsync(query.LicenseKey, cancellationToken);
            if (matched is null)
                return new PagedResult<LicenseListItemDto>(Array.Empty<LicenseListItemDto>(), 0, query.Page, query.PageSize);

            var detailed = await _db.Licenses.AsNoTracking()
                .Include(l => l.Customer)
                .Include(l => l.Product)
                .Include(l => l.Plan)
                .Include(l => l.Activations)
                .FirstAsync(l => l.Id == matched.Id, cancellationToken);

            return new PagedResult<LicenseListItemDto>(
                new[] { MapListItem(detailed) },
                1,
                query.Page,
                query.PageSize);
        }

        var q = _db.Licenses.AsNoTracking()
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .Include(l => l.Plan)
            .Include(l => l.Activations)
            .Where(l => !l.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.LicenseKeyPrefix))
            q = q.Where(l => l.LicenseKeyPrefix == query.LicenseKeyPrefix.Trim());

        if (query.CustomerId.HasValue)
            q = q.Where(l => l.CustomerId == query.CustomerId.Value);

        if (query.ProductId.HasValue)
            q = q.Where(l => l.ProductId == query.ProductId.Value);

        if (query.PlanId.HasValue)
            q = q.Where(l => l.PlanId == query.PlanId.Value);

        if (query.Status.HasValue)
            q = q.Where(l => l.Status == query.Status.Value);

        if (query.ExpiringWithinDays.HasValue)
        {
            var threshold = _clock.UtcNow.AddDays(query.ExpiringWithinDays.Value);
            q = q.Where(l => l.ExpirationDateUtc <= threshold && l.Status == LicenseStatus.Active);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LicenseListItemDto>(
            items.Select(MapListItem).ToList(),
            total,
            query.Page,
            query.PageSize);
    }

    public async Task UpdateLicenseAsync(Guid id, UpdateLicenseRequest request, string actor, string? ip, CancellationToken cancellationToken = default)
    {
        var license = await _db.Licenses
            .Include(l => l.LicenseFeatures)
            .Include(l => l.LicenseLimits)
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("License not found.");

        if (license.Status == LicenseStatus.Revoked)
            throw new ConflictException("Revoked licenses cannot be updated.");

        if (request.MaxActivations.HasValue)
        {
            var activeCount = license.Activations?.Count(a => a.Status == ActivationStatus.Active) ??
                await _db.LicenseActivations.CountAsync(a => a.LicenseId == id && a.Status == ActivationStatus.Active, cancellationToken);
            if (request.MaxActivations.Value < activeCount)
                throw new ConflictException("Max activations cannot be less than current active activations.");
            license.MaxActivations = request.MaxActivations.Value;
        }

        if (request.Description is not null)
            license.Description = request.Description;

        if (request.AutoRenewal.HasValue)
            license.AutoRenewal = request.AutoRenewal.Value;

        if (request.Features is not null)
            await ReplaceLicenseFeaturesAsync(license, request.Features, cancellationToken);

        if (request.Limits is not null)
            ReplaceLicenseLimits(license, request.Limits);

        license.UpdatedAtUtc = _clock.UtcNow;
        await _audit.LogAsync(AuditActionType.LicenseUpdated, license.Id, license.CustomerId, actor, ip, request, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ActivationResultDto> ActivateAsync(ActivateLicenseRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        await _lifecycle.EnsureExpiredLicensesUpdatedAsync(cancellationToken);
        var license = await FindByKeyAsync(request.LicenseKey, cancellationToken)
            ?? throw new NotFoundException("Invalid license key.");

        ValidateProductMatch(license, request.ProductCode);

        if (license.Status is LicenseStatus.Revoked or LicenseStatus.Suspended or LicenseStatus.Expired)
            throw new ConflictException($"License is not usable. Status: {license.Status}");

        if (license.ValidFromUtc.HasValue && license.ValidFromUtc.Value > _clock.UtcNow)
            throw new ConflictException("License is not active yet.");

        if (license.Status == LicenseStatus.Pending)
        {
            await _lifecycle.ChangeStatusAsync(license, LicenseStatus.Active, "Client", "First activation", cancellationToken);
        }

        if (license.ExpirationDateUtc <= _clock.UtcNow)
        {
            await _lifecycle.ChangeStatusAsync(license, LicenseStatus.Expired, "System", "Expired on activation attempt", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            throw new ConflictException("License has expired.");
        }

        var existing = await _db.LicenseActivations
            .FirstOrDefaultAsync(a => a.LicenseId == license.Id &&
                                      a.InstanceIdentifier == request.InstanceIdentifier &&
                                      a.Status == ActivationStatus.Active, cancellationToken);

        if (existing is not null)
        {
            existing.LastValidatedAtUtc = _clock.UtcNow;
            existing.IpAddress = ip;
            existing.ProductVersion = request.ProductVersion;
            await _db.SaveChangesAsync(cancellationToken);
            return await BuildActivationResultAsync(license, existing, true, cancellationToken);
        }

        var activeCount = await _db.LicenseActivations
            .CountAsync(a => a.LicenseId == license.Id && a.Status == ActivationStatus.Active, cancellationToken);

        if (activeCount >= license.MaxActivations)
            throw new ConflictException("Maximum activations reached for this license.");

        var activation = new LicenseActivation
        {
            Id = Guid.NewGuid(),
            LicenseId = license.Id,
            InstanceIdentifier = request.InstanceIdentifier.Trim(),
            ActivatedAtUtc = _clock.UtcNow,
            LastValidatedAtUtc = _clock.UtcNow,
            IpAddress = ip,
            Status = ActivationStatus.Active,
            ProductVersion = request.ProductVersion
        };

        _db.LicenseActivations.Add(activation);
        await _audit.LogAsync(AuditActionType.LicenseActivated, license.Id, license.CustomerId, "Client", ip,
            new { request.InstanceIdentifier }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildActivationResultAsync(license, activation, true, cancellationToken);
    }

    public async Task DeactivateAsync(DeactivateLicenseRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var license = await FindByKeyAsync(request.LicenseKey, cancellationToken)
            ?? throw new NotFoundException("Invalid license key.");

        ValidateProductMatch(license, request.ProductCode);

        var activation = await _db.LicenseActivations
            .FirstOrDefaultAsync(a => a.LicenseId == license.Id &&
                                      a.InstanceIdentifier == request.InstanceIdentifier &&
                                      a.Status == ActivationStatus.Active, cancellationToken)
            ?? throw new NotFoundException("Active activation not found for this instance.");

        activation.Status = ActivationStatus.Deactivated;
        activation.DeactivatedAtUtc = _clock.UtcNow;

        await _audit.LogAsync(AuditActionType.LicenseDeactivated, license.Id, license.CustomerId, "Client", ip,
            new { request.InstanceIdentifier }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ValidationResultDto> ValidateAsync(ValidateLicenseRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        await _lifecycle.EnsureExpiredLicensesUpdatedAsync(cancellationToken);
        var license = await FindByKeyAsync(request.LicenseKey, cancellationToken);

        if (license is null)
        {
            await _audit.LogAsync(AuditActionType.LicenseValidated, null, null, "Client", ip,
                new { valid = false, reason = "key_not_found" }, isSuspicious: true, cancellationToken: cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return ValidationResultDto.Invalid("Invalid license key.");
        }

        ValidateProductMatch(license, request.ProductCode);

        LicenseActivation? activation = null;
        if (!string.IsNullOrWhiteSpace(request.InstanceIdentifier))
        {
            activation = await _db.LicenseActivations
                .FirstOrDefaultAsync(a => a.LicenseId == license.Id &&
                                          a.InstanceIdentifier == request.InstanceIdentifier &&
                                          a.Status == ActivationStatus.Active, cancellationToken);
        }

        var isValid = license.Status == LicenseStatus.Active &&
                      (!license.ValidFromUtc.HasValue || license.ValidFromUtc.Value <= _clock.UtcNow) &&
                      license.ExpirationDateUtc > _clock.UtcNow &&
                      (string.IsNullOrWhiteSpace(request.InstanceIdentifier) || activation is not null);

        if (activation is not null)
        {
            activation.LastValidatedAtUtc = _clock.UtcNow;
            activation.IpAddress = ip;
            activation.ProductVersion = request.ProductVersion ?? activation.ProductVersion;
        }

        await _audit.LogAsync(AuditActionType.LicenseValidated, license.Id, license.CustomerId, "Client", ip,
            new { isValid, license.Status, request.InstanceIdentifier }, cancellationToken: cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (!isValid)
        {
            var reason = license.Status switch
            {
                LicenseStatus.Suspended => "License is suspended.",
                LicenseStatus.Revoked => "License is revoked.",
                LicenseStatus.Expired => "License has expired.",
                _ when license.ValidFromUtc.HasValue && license.ValidFromUtc.Value > _clock.UtcNow => "License is not active yet.",
                LicenseStatus.Pending => "License is not activated yet.",
                _ when license.ExpirationDateUtc <= _clock.UtcNow => "License has expired.",
                _ when activation is null && !string.IsNullOrWhiteSpace(request.InstanceIdentifier) => "Instance is not activated.",
                _ => "License is not valid."
            };
            return ValidationResultDto.Invalid(reason, license.Status, license.ExpirationDateUtc);
        }

        var features = await GetEnabledFeaturesAsync(license.Id, cancellationToken);
        var limits = await _db.LicenseLimits.AsNoTracking()
            .Where(l => l.LicenseId == license.Id)
            .ToDictionaryAsync(l => l.LimitKey, l => l.LimitValue, cancellationToken);

        return ValidationResultDto.Valid(
            license.Status,
            license.ExpirationDateUtc,
            features,
            limits);
    }

    public async Task<IReadOnlyList<ActivationDto>> GetActivationsAsync(Guid licenseId, CancellationToken cancellationToken = default)
    {
        return await _db.LicenseActivations.AsNoTracking()
            .Where(a => a.LicenseId == licenseId)
            .OrderByDescending(a => a.ActivatedAtUtc)
            .Select(a => new ActivationDto(
                a.Id,
                a.InstanceIdentifier,
                a.ActivatedAtUtc,
                a.LastValidatedAtUtc,
                a.IpAddress,
                a.Status.ToString(),
                a.DeactivatedAtUtc,
                a.ProductVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StatusHistoryDto>> GetHistoryAsync(Guid licenseId, CancellationToken cancellationToken = default)
    {
        return await _db.LicenseStatusHistories.AsNoTracking()
            .Where(h => h.LicenseId == licenseId)
            .OrderByDescending(h => h.ChangedAtUtc)
            .Select(h => new StatusHistoryDto(
                h.Id,
                h.FromStatus.ToString(),
                h.ToStatus.ToString(),
                h.Reason,
                h.ChangedBy,
                h.ChangedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LicenseListItemDto>> GetCustomerLicensesAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var items = await _db.Licenses.AsNoTracking()
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .Include(l => l.Plan)
            .Include(l => l.Activations)
            .Where(l => l.CustomerId == customerId && !l.IsDeleted)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(MapListItem).ToList();
    }

    private IQueryable<License> QueryLicenseDetails()
    {
        return _db.Licenses.AsNoTracking()
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .Include(l => l.Plan)
            .Include(l => l.LicenseFeatures).ThenInclude(lf => lf.ProductFeature)
            .Include(l => l.LicenseLimits)
            .Include(l => l.Activations);
    }

    private async Task<License?> FindByKeyAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var prefix = _keyService.GetKeyPrefix(licenseKey);
        var candidates = await _db.Licenses
            .Include(l => l.Product)
            .Where(l => !l.IsDeleted && l.LicenseKeyPrefix == prefix)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(l => _keyService.VerifyLicenseKey(licenseKey, l.LicenseKeyHash));
    }

    private static void ValidateProductMatch(License license, string productCode)
    {
        if (!string.Equals(license.Product.Code, productCode, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Product identifier does not match this license.");
    }

    private async Task ValidateCreateRequestAsync(CreateLicenseRequest request, CancellationToken cancellationToken)
    {
        if (request.ExpirationDateUtc <= _clock.UtcNow)
            throw new ValidationException("Expiration date must be in the future (UTC).");

        if (request.ValidFromUtc.HasValue && request.ValidFromUtc.Value >= request.ExpirationDateUtc)
            throw new ValidationException("Valid-from date must be before expiration date.");

        if (request.ActivateImmediately && request.ValidFromUtc.HasValue && request.ValidFromUtc.Value > _clock.UtcNow)
            throw new ValidationException("A license cannot be activated before its valid-from date.");

        if (request.MaxActivations < 1)
            throw new ValidationException("Max activations must be at least 1.");

        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId && !c.IsDeleted, cancellationToken);
        if (!customerExists)
            throw new NotFoundException("Customer not found.");

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive && !p.IsDeleted, cancellationToken);
        if (product is null)
            throw new NotFoundException("Product not found or inactive.");

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId && p.ProductId == request.ProductId && p.IsActive, cancellationToken);
        if (plan is null)
            throw new NotFoundException("Plan not found for the specified product.");

        if (request.SubscriptionId.HasValue)
        {
            var subOk = await _db.Subscriptions.AnyAsync(s => s.Id == request.SubscriptionId.Value && s.CustomerId == request.CustomerId, cancellationToken);
            if (!subOk)
                throw new ValidationException("Subscription does not belong to the customer.");
        }
    }

    private async Task CopyPlanFeaturesAndLimitsAsync(License license, CreateLicenseRequest request, CancellationToken cancellationToken)
    {
        var planFeatures = await _db.PlanFeatures
            .Include(pf => pf.ProductFeature)
            .Where(pf => pf.PlanId == license.PlanId && pf.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var pf in planFeatures)
        {
            license.LicenseFeatures.Add(new LicenseFeature
            {
                LicenseId = license.Id,
                ProductFeatureId = pf.ProductFeatureId,
                IsEnabled = true,
                LimitValue = pf.LimitValue
            });
        }

        if (request.Features is not null)
        {
            foreach (var feature in request.Features)
            {
                var productFeature = await _db.ProductFeatures
                    .FirstOrDefaultAsync(f => f.ProductId == license.ProductId && f.FeatureKey == feature.FeatureKey, cancellationToken);
                if (productFeature is null)
                    throw new ValidationException($"Unknown feature: {feature.FeatureKey}");

                var existing = license.LicenseFeatures.FirstOrDefault(lf => lf.ProductFeatureId == productFeature.Id);
                if (existing is null)
                {
                    license.LicenseFeatures.Add(new LicenseFeature
                    {
                        LicenseId = license.Id,
                        ProductFeatureId = productFeature.Id,
                        IsEnabled = feature.IsEnabled,
                        LimitValue = feature.LimitValue
                    });
                }
                else
                {
                    existing.IsEnabled = feature.IsEnabled;
                    existing.LimitValue = feature.LimitValue;
                }
            }
        }

        var limits = request.Limits ?? new List<LicenseLimitInput>();
        ValidateLimits(limits);
        foreach (var limit in limits)
        {
            license.LicenseLimits.Add(new LicenseLimit
            {
                Id = Guid.NewGuid(),
                LicenseId = license.Id,
                LimitKey = limit.LimitKey,
                LimitValue = limit.LimitValue,
                Unit = limit.Unit
            });
        }
    }

    private async Task ReplaceLicenseFeaturesAsync(License license, IReadOnlyList<LicenseFeatureInput> features, CancellationToken cancellationToken)
    {
        _db.LicenseFeatures.RemoveRange(license.LicenseFeatures);
        license.LicenseFeatures.Clear();

        foreach (var feature in features)
        {
            var productFeature = await _db.ProductFeatures
                .FirstOrDefaultAsync(f => f.ProductId == license.ProductId && f.FeatureKey == feature.FeatureKey, cancellationToken)
                ?? throw new ValidationException($"Unknown feature: {feature.FeatureKey}");

            license.LicenseFeatures.Add(new LicenseFeature
            {
                LicenseId = license.Id,
                ProductFeatureId = productFeature.Id,
                IsEnabled = feature.IsEnabled,
                LimitValue = feature.LimitValue
            });
        }
    }

    private static void ReplaceLicenseLimits(License license, IReadOnlyList<LicenseLimitInput> limits)
    {
        ValidateLimits(limits);
        license.LicenseLimits.Clear();
        foreach (var limit in limits)
        {
            license.LicenseLimits.Add(new LicenseLimit
            {
                Id = Guid.NewGuid(),
                LicenseId = license.Id,
                LimitKey = limit.LimitKey,
                LimitValue = limit.LimitValue,
                Unit = limit.Unit
            });
        }
    }

    private static void ValidateLimits(IReadOnlyList<LicenseLimitInput> limits)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var limit in limits)
        {
            if (string.IsNullOrWhiteSpace(limit.LimitKey))
                throw new ValidationException("Limit key is required.");

            if (!keys.Add(limit.LimitKey.Trim()))
                throw new ValidationException($"Duplicate limit: {limit.LimitKey}");
        }
    }

    private async Task<Dictionary<string, string>> GetEnabledFeaturesAsync(Guid licenseId, CancellationToken cancellationToken)
    {
        return await _db.LicenseFeatures.AsNoTracking()
            .Include(lf => lf.ProductFeature)
            .Where(lf => lf.LicenseId == licenseId && lf.IsEnabled)
            .ToDictionaryAsync(
                lf => lf.ProductFeature.FeatureKey,
                lf => lf.LimitValue ?? "true",
                cancellationToken);
    }

    private async Task<ActivationResultDto> BuildActivationResultAsync(
        License license,
        LicenseActivation activation,
        bool success,
        CancellationToken cancellationToken)
    {
        var features = await GetEnabledFeaturesAsync(license.Id, cancellationToken);
        var limits = await _db.LicenseLimits.AsNoTracking()
            .Where(l => l.LicenseId == license.Id)
            .ToDictionaryAsync(l => l.LimitKey, l => l.LimitValue, cancellationToken);

        return new ActivationResultDto(
            success,
            license.Id,
            activation.Id,
            license.Status.ToString(),
            license.ExpirationDateUtc,
            features,
            limits);
    }

    private LicenseListItemDto MapListItem(License license)
    {
        var activeCount = license.Activations.Count(a => a.Status == ActivationStatus.Active);
        return new LicenseListItemDto(
            license.Id,
            license.LicenseKeyPrefix,
            license.CustomerId,
            license.Customer.Name,
            license.ProductId,
            license.Product.Name,
            license.PlanId,
            license.Plan.Name,
            license.Status.ToString(),
            license.ValidFromUtc,
            license.ExpirationDateUtc,
            license.MaxActivations,
            activeCount,
            license.AutoRenewal,
            license.CreatedAtUtc);
    }

    private static LicenseDetailDto MapDetail(License license)
    {
        var activeCount = license.Activations.Count(a => a.Status == ActivationStatus.Active);
        return new LicenseDetailDto(
            license.Id,
            license.LicenseKeyPrefix,
            license.CustomerId,
            license.Customer.Name,
            license.ProductId,
            license.Product.Code,
            license.Product.Name,
            license.PlanId,
            license.Plan.Name,
            license.SubscriptionId,
            license.Status.ToString(),
            license.ValidFromUtc,
            license.ExpirationDateUtc,
            license.MaxActivations,
            activeCount,
            license.AutoRenewal,
            license.Description,
            license.CreatedBy,
            license.CreatedAtUtc,
            license.LicenseFeatures
                .Where(f => f.IsEnabled)
                .Select(f => new FeatureDto(f.ProductFeature.FeatureKey, f.ProductFeature.Name, f.LimitValue))
                .ToList(),
            license.LicenseLimits
                .Select(l => new LimitDto(l.LimitKey, l.LimitValue, l.Unit))
                .ToList());
    }
}
