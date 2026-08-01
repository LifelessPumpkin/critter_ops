using Microsoft.EntityFrameworkCore;
using skipper_api.Domain.Animals;
using skipper_api.Domain.Enclosures;
using skipper_api.Domain.EnclosureTimeline;

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

    public DbSet<Animal> Animals => Set<Animal>();

    public DbSet<Enclosure> Enclosures => Set<Enclosure>();

    public DbSet<EnclosureTimelineEvent> EnclosureTimelineEvents => Set<EnclosureTimelineEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically applies all IEntityTypeConfiguration<T> implementations in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfessorDbContext).Assembly);
    }
}
