using Licensing.Domain.Common;

namespace Licensing.Domain.Entities;

public class Product : EntityBase
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductFeature> Features { get; set; } = new List<ProductFeature>();
    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
}
