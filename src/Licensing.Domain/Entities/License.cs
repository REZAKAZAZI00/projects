using Licensing.Domain.Common;
using Licensing.Domain.Enums;

namespace Licensing.Domain.Entities;

public class License : EntityBase
{
    public Guid Id { get; set; }
    public string LicenseKeyHash { get; set; } = string.Empty;
    public string LicenseKeyPrefix { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public Guid PlanId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public LicenseStatus Status { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime ExpirationDateUtc { get; set; }
    public int MaxActivations { get; set; }
    public bool AutoRenewal { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }

    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
    public Subscription? Subscription { get; set; }
    public ICollection<LicenseFeature> LicenseFeatures { get; set; } = new List<LicenseFeature>();
    public ICollection<LicenseLimit> LicenseLimits { get; set; } = new List<LicenseLimit>();
    public ICollection<LicenseActivation> Activations { get; set; } = new List<LicenseActivation>();
    public ICollection<LicenseStatusHistory> StatusHistory { get; set; } = new List<LicenseStatusHistory>();
}
