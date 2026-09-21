using Licensing.Domain.Enums;

namespace Licensing.Application.Abstractions;

public interface IAuditService
{
    Task LogAsync(
        AuditActionType actionType,
        Guid? licenseId,
        Guid? customerId,
        string? actor,
        string? ipAddress,
        object? details = null,
        bool isSuspicious = false,
        CancellationToken cancellationToken = default);
}
