using Licensing.Application.Abstractions;
using Licensing.Infrastructure.Persistence;
using Licensing.Infrastructure.Security;
using Licensing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Licensing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LicensingDatabase")
            ?? throw new InvalidOperationException("Connection string 'LicensingDatabase' is not configured.");

        var serverVersion = ServerVersion.Parse("10.11.6-mariadb");
        services.AddDbContext<LicensingDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        services.AddScoped<ILicensingDbContext>(sp => sp.GetRequiredService<LicensingDbContext>());
        services.AddSingleton<ILicenseKeyService, LicenseKeyService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
