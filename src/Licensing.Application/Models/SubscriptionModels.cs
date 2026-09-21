using Licensing.Domain.Enums;

namespace Licensing.Application.Models;

public record CreateSubscriptionRequest(
    Guid CustomerId,
    Guid ProductId,
    Guid PlanId,
    DateTime StartDateUtc,
    DateTime ExpirationDateUtc,
    SubscriptionStatus Status = SubscriptionStatus.Active,
    bool CreateLicense = true,
    int? MaxActivations = null,
    string? MetadataJson = null);

public record RenewSubscriptionRequest(
    DateTime NewExpirationDateUtc,
    string? Notes,
    bool SyncLicenseExpiration = true);

public record SubscriptionDetailDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid ProductId,
    string ProductName,
    Guid PlanId,
    string PlanName,
    DateTime StartDateUtc,
    DateTime ExpirationDateUtc,
    string Status,
    Guid? CurrentLicenseId,
    string? MetadataJson,
    TimeSpan Remaining,
    IReadOnlyList<SubscriptionRenewalDto> Renewals,
    IReadOnlyList<SubscriptionStatusHistoryDto> StatusHistory);

public record SubscriptionListItemDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid ProductId,
    string ProductName,
    Guid PlanId,
    string PlanName,
    DateTime StartDateUtc,
    DateTime ExpirationDateUtc,
    string Status,
    Guid? CurrentLicenseId,
    TimeSpan Remaining);

public record SubscriptionRenewalDto(
    Guid Id,
    DateTime PreviousExpirationUtc,
    DateTime NewExpirationUtc,
    DateTime RenewedAtUtc,
    string? RenewedBy,
    string? Notes);

public record SubscriptionStatusHistoryDto(
    Guid Id,
    string FromStatus,
    string ToStatus,
    string? Reason,
    string? ChangedBy,
    DateTime ChangedAtUtc);

public record SubscriptionQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CustomerId = null,
    Guid? ProductId = null,
    SubscriptionStatus? Status = null);
