namespace Licensing.Application.Models;

public record CreateProductRequest(string Code, string Name, string? Description);
public record UpdateProductRequest(string? Name, string? Description, bool? IsActive);

public record CreatePlanRequest(string Code, string Name, string? Description, int DefaultMaxActivations, int? DefaultDurationDays);
public record UpdatePlanRequest(string? Name, string? Description, int? DefaultMaxActivations, int? DefaultDurationDays, bool? IsActive);

public record CreateProductFeatureRequest(string FeatureKey, string Name, string? Description);
public record UpdateProductFeatureRequest(string? Name, string? Description, bool? IsActive);

public record SetPlanFeaturesRequest(IReadOnlyList<PlanFeatureInput> Features);
public record PlanFeatureInput(string FeatureKey, bool IsEnabled, string? LimitValue);
