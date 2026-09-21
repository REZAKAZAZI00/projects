using Licensing.Domain.Entities;
using Licensing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licensing.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalCustomerId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.ExternalCustomerId).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class ProductFeatureConfiguration : IEntityTypeConfiguration<ProductFeature>
{
    public void Configure(EntityTypeBuilder<ProductFeature> builder)
    {
        builder.ToTable("ProductFeatures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FeatureKey).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.FeatureKey }).IsUnique();
    }
}

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.Code }).IsUnique();
    }
}

public class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("PlanFeatures");
        builder.HasKey(x => new { x.PlanId, x.ProductFeatureId });
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasOne(x => x.CurrentLicense).WithMany().HasForeignKey(x => x.CurrentLicenseId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("Licenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LicenseKeyHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.LicenseKeyPrefix).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => x.LicenseKeyPrefix);
        builder.HasIndex(x => new { x.CustomerId, x.ProductId });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class LicenseFeatureConfiguration : IEntityTypeConfiguration<LicenseFeature>
{
    public void Configure(EntityTypeBuilder<LicenseFeature> builder)
    {
        builder.ToTable("LicenseFeatures");
        builder.HasKey(x => new { x.LicenseId, x.ProductFeatureId });
    }
}

public class LicenseLimitConfiguration : IEntityTypeConfiguration<LicenseLimit>
{
    public void Configure(EntityTypeBuilder<LicenseLimit> builder)
    {
        builder.ToTable("LicenseLimits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LimitKey).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.LicenseId, x.LimitKey }).IsUnique();
    }
}

public class LicenseActivationConfiguration : IEntityTypeConfiguration<LicenseActivation>
{
    public void Configure(EntityTypeBuilder<LicenseActivation> builder)
    {
        builder.ToTable("LicenseActivations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InstanceIdentifier).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => new { x.LicenseId, x.InstanceIdentifier, x.Status });
    }
}

public class LicenseStatusHistoryConfiguration : IEntityTypeConfiguration<LicenseStatusHistory>
{
    public void Configure(EntityTypeBuilder<LicenseStatusHistory> builder)
    {
        builder.ToTable("LicenseStatusHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasConversion<int>();
        builder.Property(x => x.ToStatus).HasConversion<int>();
    }
}

public class SubscriptionRenewalConfiguration : IEntityTypeConfiguration<SubscriptionRenewal>
{
    public void Configure(EntityTypeBuilder<SubscriptionRenewal> builder)
    {
        builder.ToTable("SubscriptionRenewals");
        builder.HasKey(x => x.Id);
    }
}

public class SubscriptionStatusHistoryConfiguration : IEntityTypeConfiguration<SubscriptionStatusHistory>
{
    public void Configure(EntityTypeBuilder<SubscriptionStatusHistory> builder)
    {
        builder.ToTable("SubscriptionStatusHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasConversion<int>();
        builder.Property(x => x.ToStatus).HasConversion<int>();
        builder.HasIndex(x => new { x.SubscriptionId, x.ChangedAtUtc });
    }
}

public class LicenseAuditLogConfiguration : IEntityTypeConfiguration<LicenseAuditLog>
{
    public void Configure(EntityTypeBuilder<LicenseAuditLog> builder)
    {
        builder.ToTable("LicenseAuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionType).HasConversion<int>();
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("AdminUsers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique();
    }
}
