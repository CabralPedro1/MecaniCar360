using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MecaniCar360.Data;

// EF tooling must not execute web startup/seed. Configuration is read; construction opens no SQL connection.
public sealed class MecaniCarContextDesignFactory : IDesignTimeDbContextFactory<MecaniCarContext>
{
    public MecaniCarContext CreateDbContext(string[] args)
    {
        var entorno = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").AddJsonFile($"appsettings.{entorno}.json", optional: true);
        if (entorno == "Development") config.AddUserSecrets<Program>(optional: true);
        config.AddEnvironmentVariables();
        return new(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer(config.Build().GetConnectionString("DefaultConnection")).Options);
    }
}
