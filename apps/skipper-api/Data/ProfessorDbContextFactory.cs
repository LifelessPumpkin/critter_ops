using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace skipper_api.Data;

/// <summary>
/// Creates ProfessorDbContext for EF Core design-time tooling without starting the web host.
/// </summary>
public class ProfessorDbContextFactory : IDesignTimeDbContextFactory<ProfessorDbContext>
{
    public ProfessorDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("ProfessorDb");

        var optionsBuilder = new DbContextOptionsBuilder<ProfessorDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ProfessorDbContext(optionsBuilder.Options);
    }
}
