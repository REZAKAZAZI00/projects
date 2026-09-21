namespace Licensing.Domain.Entities;

public class LicenseFeature
{
    public Guid LicenseId { get; set; }
    public Guid ProductFeatureId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? LimitValue { get; set; }

    public License License { get; set; } = null!;
    public ProductFeature ProductFeature { get; set; } = null!;
}
