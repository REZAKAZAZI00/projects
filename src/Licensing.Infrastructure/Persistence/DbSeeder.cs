using Licensing.Domain.Entities;
using Licensing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Licensing.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(LicensingDbContext db, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!await db.AdminUsers.AnyAsync(cancellationToken))
        {
            var password = configuration["Seed:AdminPassword"] ?? "ChangeMe!123";
            db.AdminUsers.Add(new AdminUser
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = configuration["Seed:AdminUsername"] ?? "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Admin",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await db.Products.AnyAsync(cancellationToken))
        {
            var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var trialPlanId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var proPlanId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var customerId = Guid.Parse("55555555-5555-5555-5555-555555555555");

            var featureReporting = Guid.Parse("66666666-6666-6666-6666-666666666666");
            var featureApi = Guid.Parse("77777777-7777-7777-7777-777777777777");

            db.Products.Add(new Product
            {
                Id = productId,
                Code = "PRODUCT-A",
                Name = "Product A",
                Description = "Sample product for licensing demo",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });

            db.ProductFeatures.AddRange(
                new ProductFeature
                {
                    Id = featureReporting,
                    ProductId = productId,
                    FeatureKey = "advanced_reporting",
                    Name = "Advanced Reporting",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new ProductFeature
                {
                    Id = featureApi,
                    ProductId = productId,
                    FeatureKey = "api_access",
                    Name = "API Access",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });

            db.Plans.AddRange(
                new Plan
                {
                    Id = trialPlanId,
                    ProductId = productId,
                    Code = "TRIAL",
                    Name = "Trial",
                    DefaultMaxActivations = 1,
                    DefaultDurationDays = 14,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Plan
                {
                    Id = proPlanId,
                    ProductId = productId,
                    Code = "PROFESSIONAL",
                    Name = "Professional",
                    DefaultMaxActivations = 3,
                    DefaultDurationDays = 365,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });

            db.PlanFeatures.AddRange(
                new PlanFeature { PlanId = trialPlanId, ProductFeatureId = featureApi, IsEnabled = true },
                new PlanFeature { PlanId = proPlanId, ProductFeatureId = featureApi, IsEnabled = true },
                new PlanFeature { PlanId = proPlanId, ProductFeatureId = featureReporting, IsEnabled = true });

            db.Customers.Add(new Customer
            {
                Id = customerId,
                ExternalCustomerId = "CUST-001",
                Name = "Sample Customer",
                Email = "customer@example.com",
                CreatedAtUtc = DateTime.UtcNow
            });

            var subscriptionId = Guid.Parse("88888888-8888-8888-8888-888888888888");
            db.Subscriptions.Add(new Subscription
            {
                Id = subscriptionId,
                CustomerId = customerId,
                ProductId = productId,
                PlanId = proPlanId,
                StartDateUtc = DateTime.UtcNow,
                ExpirationDateUtc = DateTime.UtcNow.AddYears(1),
                Status = SubscriptionStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
