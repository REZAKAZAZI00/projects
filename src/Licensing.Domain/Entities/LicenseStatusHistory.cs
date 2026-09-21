using Licensing.Domain.Enums;

namespace Licensing.Domain.Entities;

public class LicenseStatusHistory
{
    public Guid Id { get; set; }
    public Guid LicenseId { get; set; }
    public LicenseStatus FromStatus { get; set; }
    public LicenseStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAtUtc { get; set; }

    public License License { get; set; } = null!;
}
