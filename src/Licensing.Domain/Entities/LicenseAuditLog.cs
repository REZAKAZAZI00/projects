using Licensing.Domain.Enums;

namespace Licensing.Domain.Entities;

public class LicenseAuditLog
{
    public Guid Id { get; set; }
    public AuditActionType ActionType { get; set; }
    public Guid? LicenseId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Actor { get; set; }
    public string? IpAddress { get; set; }
    public string? DetailsJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsSuspicious { get; set; }
}
