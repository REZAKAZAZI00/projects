using Licensing.Application.Services;

namespace Licensing.Api.BackgroundServices;

public class LicenseExpirationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LicenseExpirationBackgroundService> _logger;

    public LicenseExpirationBackgroundService(IServiceProvider serviceProvider, ILogger<LicenseExpirationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var lifecycle = scope.ServiceProvider.GetRequiredService<LicenseLifecycleService>();
                await lifecycle.EnsureExpiredLicensesUpdatedAsync(stoppingToken);
                var subscriptions = scope.ServiceProvider.GetRequiredService<SubscriptionService>();
                await subscriptions.EnsureSubscriptionStatusesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process license expirations.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
