namespace Licensing.Domain.Entities;

public class SubscriptionRenewal
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public DateTime PreviousExpirationUtc { get; set; }
    public DateTime NewExpirationUtc { get; set; }
    public DateTime RenewedAtUtc { get; set; }
    public string? RenewedBy { get; set; }
    public string? Notes { get; set; }

    public Subscription Subscription { get; set; } = null!;
}
