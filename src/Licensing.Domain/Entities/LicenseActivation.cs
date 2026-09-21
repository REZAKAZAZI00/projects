using Licensing.Domain.Enums;

namespace Licensing.Domain.Entities;

public class LicenseActivation
{
    public Guid Id { get; set; }
    public Guid LicenseId { get; set; }
    public string InstanceIdentifier { get; set; } = string.Empty;
    public DateTime ActivatedAtUtc { get; set; }
    public DateTime? LastValidatedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public ActivationStatus Status { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }
    public string? ProductVersion { get; set; }

    public License License { get; set; } = null!;
}
