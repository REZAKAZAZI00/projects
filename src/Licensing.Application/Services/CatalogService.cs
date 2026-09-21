using Licensing.Application.Models;
using Licensing.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Application.Services;

public class CatalogService
{
    private readonly ILicensingDbContext _db;

    public CatalogService(ILicensingDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Code, p.Name, p.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlanDto>> GetPlansAsync(Guid? productId, CancellationToken cancellationToken = default)
    {
        var q = _db.Plans.AsNoTracking().Where(p => p.IsActive && !p.IsDeleted);
        if (productId.HasValue)
            q = q.Where(p => p.ProductId == productId.Value);

        return await q.OrderBy(p => p.Name)
            .Select(p => new PlanDto(
                p.Id,
                p.ProductId,
                p.Code,
                p.Name,
                p.Description,
                p.DefaultMaxActivations,
                p.DefaultDurationDays))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeatureDto>> GetProductFeaturesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _db.ProductFeatures.AsNoTracking()
            .Where(f => f.ProductId == productId && f.IsActive && !f.IsDeleted)
            .OrderBy(f => f.FeatureKey)
            .Select(f => new FeatureDto(f.FeatureKey, f.Name, null))
            .ToListAsync(cancellationToken);
    }
}
