using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Licensing.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LicensingDbContext>
{
    public LicensingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LicensingDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("LICENSING_DB")
            ?? "Server=localhost;Port=3306;Database=LicensingDb;User=root;Password=root;";

        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("10.11.6-mariadb"));
        return new LicensingDbContext(optionsBuilder.Options);
    }
}
