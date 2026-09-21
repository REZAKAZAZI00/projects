using FluentValidation;
using Licensing.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Licensing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LicenseService>();
        services.AddScoped<LicenseLifecycleService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<CatalogAdminService>();
        services.AddScoped<AuditQueryService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
