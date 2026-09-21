namespace Licensing.Domain.Entities;

public class PlanFeature
{
    public Guid PlanId { get; set; }
    public Guid ProductFeatureId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? LimitValue { get; set; }

    public Plan Plan { get; set; } = null!;
    public ProductFeature ProductFeature { get; set; } = null!;
}
