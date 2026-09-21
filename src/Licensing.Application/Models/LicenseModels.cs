namespace Licensing.Application.Models;

public record CreateLicenseRequest(
    Guid CustomerId,
    Guid ProductId,
    Guid PlanId,
    DateTime ExpirationDateUtc,
    int MaxActivations,
    Guid? SubscriptionId = null,
    DateTime? ValidFromUtc = null,
    bool ActivateImmediately = false,
    bool AutoRenewal = false,
    string? Description = null,
    IReadOnlyList<LicenseFeatureInput>? Features = null,
    IReadOnlyList<LicenseLimitInput>? Limits = null);

public record CreateLicenseResult(Guid LicenseId, string LicenseKey);

public record UpdateLicenseRequest(
    int? MaxActivations = null,
    bool? AutoRenewal = null,
    string? Description = null,
    IReadOnlyList<LicenseFeatureInput>? Features = null,
    IReadOnlyList<LicenseLimitInput>? Limits = null);

public record LicenseFeatureInput(string FeatureKey, bool IsEnabled, string? LimitValue);
public record LicenseLimitInput(string LimitKey, string LimitValue, string? Unit);

public record ActivateLicenseRequest(string LicenseKey, string ProductCode, string InstanceIdentifier, string? ProductVersion);
public record DeactivateLicenseRequest(string LicenseKey, string ProductCode, string InstanceIdentifier);
public record ValidateLicenseRequest(string LicenseKey, string ProductCode, string? InstanceIdentifier, string? ProductVersion);

public record LicenseQuery(
    int Page = 1,
    int PageSize = 20,
    string? LicenseKey = null,
    string? LicenseKeyPrefix = null,
    Guid? CustomerId = null,
    Guid? ProductId = null,
    Guid? PlanId = null,
    Domain.Enums.LicenseStatus? Status = null,
    int? ExpiringWithinDays = null);

public record AuditQuery(int Page = 1, int PageSize = 50, Guid? LicenseId = null, Guid? CustomerId = null, bool? SuspiciousOnly = null);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record ProductDto(Guid Id, string Code, string Name, string? Description);
public record PlanDto(Guid Id, Guid ProductId, string Code, string Name, string? Description, int DefaultMaxActivations, int? DefaultDurationDays);
public record FeatureDto(string FeatureKey, string Name, string? LimitValue);
public record LimitDto(string LimitKey, string LimitValue, string? Unit);

public record LicenseListItemDto(
    Guid Id,
    string LicenseKeyPrefix,
    Guid CustomerId,
    string CustomerName,
    Guid ProductId,
    string ProductName,
    Guid PlanId,
    string PlanName,
    string Status,
    DateTime? ValidFromUtc,
    DateTime ExpirationDateUtc,
    int MaxActivations,
    int ActiveActivationCount,
    bool AutoRenewal,
    DateTime CreatedAtUtc);

public record LicenseDetailDto(
    Guid Id,
    string LicenseKeyPrefix,
    Guid CustomerId,
    string CustomerName,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid PlanId,
    string PlanName,
    Guid? SubscriptionId,
    string Status,
    DateTime? ValidFromUtc,
    DateTime ExpirationDateUtc,
    int MaxActivations,
    int ActiveActivationCount,
    bool AutoRenewal,
    string? Description,
    string? CreatedBy,
    DateTime CreatedAtUtc,
    IReadOnlyList<FeatureDto> Features,
    IReadOnlyList<LimitDto> Limits);

public record ActivationDto(
    Guid Id,
    string InstanceIdentifier,
    DateTime ActivatedAtUtc,
    DateTime? LastValidatedAtUtc,
    string? IpAddress,
    string Status,
    DateTime? DeactivatedAtUtc,
    string? ProductVersion);

public record StatusHistoryDto(
    Guid Id,
    string FromStatus,
    string ToStatus,
    string? Reason,
    string? ChangedBy,
    DateTime ChangedAtUtc);

public record ActivationResultDto(
    bool Success,
    Guid LicenseId,
    Guid ActivationId,
    string Status,
    DateTime ExpirationDateUtc,
    IReadOnlyDictionary<string, string> Features,
    IReadOnlyDictionary<string, string> Limits);

public record ValidationResultDto(
    bool IsValid,
    string? Message,
    string? Status,
    DateTime? ExpirationDateUtc,
    TimeSpan? Remaining,
    IReadOnlyDictionary<string, string>? Features,
    IReadOnlyDictionary<string, string>? Limits)
{
    public static ValidationResultDto Invalid(string message, Domain.Enums.LicenseStatus? status = null, DateTime? expiration = null) =>
        new(false, message, status?.ToString(), expiration, expiration.HasValue ? expiration.Value - DateTime.UtcNow : null, null, null);

    public static ValidationResultDto Valid(
        Domain.Enums.LicenseStatus status,
        DateTime expiration,
        IReadOnlyDictionary<string, string> features,
        IReadOnlyDictionary<string, string> limits) =>
        new(true, null, status.ToString(), expiration, expiration - DateTime.UtcNow, features, limits);
}

public record AuditLogDto(
    Guid Id,
    string ActionType,
    Guid? LicenseId,
    Guid? CustomerId,
    string? Actor,
    string? IpAddress,
    string? DetailsJson,
    bool IsSuspicious,
    DateTime CreatedAtUtc);
