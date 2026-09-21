using Licensing.Domain.Common;

namespace Licensing.Domain.Entities;

public class Customer : EntityBase
{
    public Guid Id { get; set; }
    public string ExternalCustomerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<License> Licenses { get; set; } = new List<License>();
}
