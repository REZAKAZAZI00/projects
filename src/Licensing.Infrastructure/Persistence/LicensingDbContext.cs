using Licensing.Application.Abstractions;
using Licensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Infrastructure.Persistence;

public class LicensingDbContext : DbContext, ILicensingDbContext
{
    public LicensingDbContext(DbContextOptions<LicensingDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductFeature> ProductFeatures => Set<ProductFeature>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<LicenseFeature> LicenseFeatures => Set<LicenseFeature>();
    public DbSet<LicenseLimit> LicenseLimits => Set<LicenseLimit>();
    public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();
    public DbSet<LicenseStatusHistory> LicenseStatusHistories => Set<LicenseStatusHistory>();
    public DbSet<SubscriptionRenewal> SubscriptionRenewals => Set<SubscriptionRenewal>();
    public DbSet<SubscriptionStatusHistory> SubscriptionStatusHistories => Set<SubscriptionStatusHistory>();
    public DbSet<LicenseAuditLog> LicenseAuditLogs => Set<LicenseAuditLog>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LicensingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
