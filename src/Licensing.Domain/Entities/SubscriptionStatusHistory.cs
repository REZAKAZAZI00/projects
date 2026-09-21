using Licensing.Domain.Enums;

namespace Licensing.Domain.Entities;

public class SubscriptionStatusHistory
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public SubscriptionStatus FromStatus { get; set; }
    public SubscriptionStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAtUtc { get; set; }

    public Subscription Subscription { get; set; } = null!;
}
