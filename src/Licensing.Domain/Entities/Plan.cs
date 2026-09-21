using Licensing.Domain.Common;

namespace Licensing.Domain.Entities;

public class Plan : EntityBase
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultMaxActivations { get; set; } = 1;
    public int? DefaultDurationDays { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
    public ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
}
