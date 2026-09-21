namespace Licensing.Domain.Entities;

public class LicenseLimit
{
    public Guid Id { get; set; }
    public Guid LicenseId { get; set; }
    public string LimitKey { get; set; } = string.Empty;
    public string LimitValue { get; set; } = string.Empty;
    public string? Unit { get; set; }

    public License License { get; set; } = null!;
}
