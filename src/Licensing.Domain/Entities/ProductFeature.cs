using Licensing.Domain.Common;

namespace Licensing.Domain.Entities;

public class ProductFeature : EntityBase
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
}
