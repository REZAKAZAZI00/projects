using Licensing.Domain.Common;
using Licensing.Domain.Enums;

namespace Licensing.Domain.Entities;

public class Subscription : EntityBase
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime ExpirationDateUtc { get; set; }
    public SubscriptionStatus Status { get; set; }
    public Guid? CurrentLicenseId { get; set; }
    public string? MetadataJson { get; set; }

    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
    public License? CurrentLicense { get; set; }
    public ICollection<SubscriptionRenewal> Renewals { get; set; } = new List<SubscriptionRenewal>();
    public ICollection<License> Licenses { get; set; } = new List<License>();
}
