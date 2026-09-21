using Licensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Application.Abstractions;

public interface ILicensingDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductFeature> ProductFeatures { get; }
    DbSet<Plan> Plans { get; }
    DbSet<PlanFeature> PlanFeatures { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<License> Licenses { get; }
    DbSet<LicenseFeature> LicenseFeatures { get; }
    DbSet<LicenseLimit> LicenseLimits { get; }
    DbSet<LicenseActivation> LicenseActivations { get; }
    DbSet<LicenseStatusHistory> LicenseStatusHistories { get; }
    DbSet<SubscriptionRenewal> SubscriptionRenewals { get; }
    DbSet<SubscriptionStatusHistory> SubscriptionStatusHistories { get; }
    DbSet<LicenseAuditLog> LicenseAuditLogs { get; }
    DbSet<AdminUser> AdminUsers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
