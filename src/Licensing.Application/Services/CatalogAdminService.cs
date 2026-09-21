using Licensing.Application.Abstractions;
using Licensing.Application.Common;
using Licensing.Application.Models;
using Licensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Application.Services;

public class CatalogAdminService
{
    private readonly ILicensingDbContext _db;
    private readonly IClock _clock;

    public CatalogAdminService(ILicensingDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Products.AnyAsync(p => p.Code == request.Code && !p.IsDeleted, cancellationToken);
        if (exists)
            throw new ConflictException("Product code already exists.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductDto(product.Id, product.Code, product.Name, product.Description);
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        if (request.Name is not null) product.Name = request.Name;
        if (request.Description is not null) product.Description = request.Description;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;
        product.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlanDto> CreatePlanAsync(Guid productId, CreatePlanRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        var exists = await _db.Plans.AnyAsync(p => p.ProductId == productId && p.Code == request.Code, cancellationToken);
        if (exists)
            throw new ConflictException("Plan code already exists for this product.");

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name,
            Description = request.Description,
            DefaultMaxActivations = request.DefaultMaxActivations,
            DefaultDurationDays = request.DefaultDurationDays,
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return new PlanDto(plan.Id, plan.ProductId, plan.Code, plan.Name, plan.Description, plan.DefaultMaxActivations, plan.DefaultDurationDays);
    }

    public async Task UpdatePlanAsync(Guid planId, UpdatePlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new NotFoundException("Plan not found.");

        if (request.Name is not null) plan.Name = request.Name;
        if (request.Description is not null) plan.Description = request.Description;
        if (request.DefaultMaxActivations.HasValue) plan.DefaultMaxActivations = request.DefaultMaxActivations.Value;
        if (request.DefaultDurationDays.HasValue) plan.DefaultDurationDays = request.DefaultDurationDays;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
        plan.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FeatureDto> CreateFeatureAsync(Guid productId, CreateProductFeatureRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        var exists = await _db.ProductFeatures.AnyAsync(f => f.ProductId == productId && f.FeatureKey == request.FeatureKey && !f.IsDeleted, cancellationToken);
        if (exists)
            throw new ConflictException("Feature key already exists.");

        var feature = new ProductFeature
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            FeatureKey = request.FeatureKey.Trim().ToLowerInvariant(),
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.ProductFeatures.Add(feature);
        await _db.SaveChangesAsync(cancellationToken);
        return new FeatureDto(feature.FeatureKey, feature.Name, null);
    }

    public async Task UpdateFeatureAsync(Guid featureId, UpdateProductFeatureRequest request, CancellationToken cancellationToken = default)
    {
        var feature = await _db.ProductFeatures.FirstOrDefaultAsync(f => f.Id == featureId && !f.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Feature not found.");

        if (request.Name is not null) feature.Name = request.Name;
        if (request.Description is not null) feature.Description = request.Description;
        if (request.IsActive.HasValue) feature.IsActive = request.IsActive.Value;
        feature.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPlanFeaturesAsync(Guid planId, SetPlanFeaturesRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _db.Plans.Include(p => p.PlanFeatures).FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new NotFoundException("Plan not found.");

        _db.PlanFeatures.RemoveRange(plan.PlanFeatures);
        plan.PlanFeatures.Clear();

        foreach (var item in request.Features)
        {
            var feature = await _db.ProductFeatures
                .FirstOrDefaultAsync(f => f.ProductId == plan.ProductId && f.FeatureKey == item.FeatureKey && f.IsActive && !f.IsDeleted, cancellationToken)
                ?? throw new ValidationException($"Unknown feature: {item.FeatureKey}");

            plan.PlanFeatures.Add(new PlanFeature
            {
                PlanId = plan.Id,
                ProductFeatureId = feature.Id,
                IsEnabled = item.IsEnabled,
                LimitValue = item.LimitValue
            });
        }

        plan.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
