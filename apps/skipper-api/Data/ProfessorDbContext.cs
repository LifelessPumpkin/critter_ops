using Microsoft.EntityFrameworkCore;

namespace skipper_api.Data;

/// <summary>
/// The primary EF Core database context for the Skipper API.
/// Named ProfessorDbContext after the professor-db service in the infrastructure stack.
/// </summary>
public class ProfessorDbContext : DbContext
{
    public ProfessorDbContext(DbContextOptions<ProfessorDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Domain entity configurations will be registered here as the schema evolves.
        // Example: modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfessorDbContext).Assembly);
    }
}
