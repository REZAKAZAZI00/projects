using Licensing.Application.Abstractions;
using Licensing.Application.Models;
using Licensing.Application.Services;
using Licensing.Domain.Entities;
using Licensing.Domain.Enums;
using Licensing.Infrastructure.Persistence;
using Licensing.Infrastructure.Security;
using Licensing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Tests;

public class LicenseServiceTests : IDisposable
{
    private readonly LicensingDbContext _db;
    private readonly LicenseService _licenseService;
    private readonly LicenseLifecycleService _lifecycleService;
    private readonly SubscriptionService _subscriptionService;
    private readonly FakeClock _clock;

    public LicenseServiceTests()
    {
        _clock = new FakeClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var options = new DbContextOptionsBuilder<LicensingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new LicensingDbContext(options);
        SeedCatalog(_db);

        var keyService = new LicenseKeyService();
        var audit = new AuditService(_db, _clock);
        _lifecycleService = new LicenseLifecycleService(_db, audit, _clock);
        _licenseService = new LicenseService(_db, keyService, audit, _clock, _lifecycleService);
        _subscriptionService = new SubscriptionService(_db, audit, _clock, _licenseService, _lifecycleService);
    }

    [Fact]
    public async Task Activate_RespectsMaxActivationLimit()
    {
        var create = await _licenseService.CreateLicenseAsync(
            new CreateLicenseRequest(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                _clock.UtcNow.AddYears(1),
                2,
                ActivateImmediately: true),
            "tester",
            null);

        await _licenseService.ActivateAsync(
            new ActivateLicenseRequest(create.LicenseKey, "PRODUCT-A", "instance-1", "1.0.0"), null);
        await _licenseService.ActivateAsync(
            new ActivateLicenseRequest(create.LicenseKey, "PRODUCT-A", "instance-2", "1.0.0"), null);

        await Assert.ThrowsAsync<Application.Common.ConflictException>(() =>
            _licenseService.ActivateAsync(
                new ActivateLicenseRequest(create.LicenseKey, "PRODUCT-A", "instance-3", "1.0.0"), null));
    }

    [Fact]
    public async Task Deactivate_FreesActivationSlot()
    {
        var create = await _licenseService.CreateLicenseAsync(
            new CreateLicenseRequest(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                _clock.UtcNow.AddYears(1),
                1,
                ActivateImmediately: true),
            "tester",
            null);

        await _licenseService.ActivateAsync(
            new ActivateLicenseRequest(create.LicenseKey, "PRODUCT-A", "instance-1", "1.0.0"), null);

        await _licenseService.DeactivateAsync(
            new DeactivateLicenseRequest(create.LicenseKey, "PRODUCT-A", "instance-1"), null);

        var second = await _licenseService.ActivateAsync(
            new ActivateLicenseRequest(create.LicenseKey, "PRODUCT-A", "instance-2", "1.0.0"), null);

        Assert.True(second.Success);
    }

    [Fact]
    public async Task Validate_ReturnsInvalidWhenSuspended()
    {
        var create = await _licenseService.CreateLicenseAsync(
            new CreateLicenseRequest(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                _clock.UtcNow.AddYears(1),
                3,
                ActivateImmediately: true),
            "tester",
            null);

        await _licenseService.ActivateAsync(
            new ActivateLicenseRequest(create.LicenseKey, "PRODUCT-A", "instance-1", "1.0.0"), null);

        await _lifecycleService.SuspendAsync(create.LicenseId, "admin", "test", null);

        var validation = await _licenseService.ValidateAsync(
            new ValidateLicenseRequest(create.LicenseKey, "PRODUCT-A", "instance-1", "1.0.0"), null);

        Assert.False(validation.IsValid);
        Assert.Equal(LicenseStatus.Suspended.ToString(), validation.Status);
    }

    [Fact]
    public async Task GracePeriod_ExtendsLinkedLicenseAndRecordsAudit()
    {
        var subscription = await _subscriptionService.CreateAsync(
            new CreateSubscriptionRequest(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                _clock.UtcNow.AddMonths(-1),
                _clock.UtcNow.AddDays(1)),
            "tester",
            null);

        await _subscriptionService.SetGracePeriodAsync(subscription.Id, 7, "admin", null);

        var updatedSubscription = await _db.Subscriptions.SingleAsync(s => s.Id == subscription.Id);
        var linkedLicense = await _db.Licenses.SingleAsync(l => l.Id == updatedSubscription.CurrentLicenseId);

        Assert.Equal(SubscriptionStatus.GracePeriod, updatedSubscription.Status);
        Assert.Equal(_clock.UtcNow.AddDays(7), updatedSubscription.ExpirationDateUtc);
        Assert.Equal(updatedSubscription.ExpirationDateUtc, linkedLicense.ExpirationDateUtc);
        Assert.Contains(_db.SubscriptionStatusHistories,
            history => history.SubscriptionId == subscription.Id &&
                       history.ToStatus == SubscriptionStatus.GracePeriod);
        Assert.Contains(_db.LicenseAuditLogs,
            log => log.ActionType == AuditActionType.SubscriptionGracePeriodStarted);
    }

    [Fact]
    public async Task FutureLicense_IsNotActivatableOrValid()
    {
        var create = await _licenseService.CreateLicenseAsync(
            new CreateLicenseRequest(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                _clock.UtcNow.AddDays(30),
                1,
                ValidFromUtc: _clock.UtcNow.AddDays(1)),
            "tester",
            null);

        await Assert.ThrowsAsync<Application.Common.ConflictException>(() =>
            _licenseService.ActivateAsync(
                new ActivateLicenseRequest(create.LicenseKey, "PRODUCT-A", "future-instance", "1.0.0"), null));

        var validation = await _licenseService.ValidateAsync(
            new ValidateLicenseRequest(create.LicenseKey, "PRODUCT-A", null, "1.0.0"), null);

        Assert.False(validation.IsValid);
        Assert.Equal("License is not active yet.", validation.Message);
    }

    [Fact]
    public async Task Create_RejectsUnknownFeaturesAndDuplicateLimits()
    {
        var unknownFeatureRequest = new CreateLicenseRequest(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            _clock.UtcNow.AddDays(30),
            1,
            Features: new[] { new LicenseFeatureInput("missing-feature", true, null) });

        await Assert.ThrowsAsync<Application.Common.ValidationException>(() =>
            _licenseService.CreateLicenseAsync(unknownFeatureRequest, "tester", null));

        var duplicateLimitRequest = unknownFeatureRequest with
        {
            Features = null,
            Limits = new[]
            {
                new LicenseLimitInput("users", "10", null),
                new LicenseLimitInput("USERS", "20", null)
            }
        };

        await Assert.ThrowsAsync<Application.Common.ValidationException>(() =>
            _licenseService.CreateLicenseAsync(duplicateLimitRequest, "tester", null));
    }

    private static void SeedCatalog(LicensingDbContext db)
    {
        db.Customers.Add(new Customer
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ExternalCustomerId = "CUST-001",
            Name = "Sample",
            Email = "sample@example.com",
            CreatedAtUtc = DateTime.UtcNow
        });
        db.Products.Add(new Product
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Code = "PRODUCT-A",
            Name = "Product A",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.Plans.Add(new Plan
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Code = "PROFESSIONAL",
            Name = "Professional",
            DefaultMaxActivations = 3,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }
}
