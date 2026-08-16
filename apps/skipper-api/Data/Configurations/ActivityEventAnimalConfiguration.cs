using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core mapping for activity-to-animal associations.
/// </summary>
public class ActivityEventAnimalConfiguration : IEntityTypeConfiguration<ActivityEventAnimal>
{
    public void Configure(EntityTypeBuilder<ActivityEventAnimal> builder)
    {
        builder.ToTable("ActivityEventAnimals");

        builder.HasKey(association => new
        {
            association.ActivityEventId,
            association.AnimalId,
            association.RelationshipType,
        });

        builder.Property(association => association.RelationshipType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(association => association.ActivityEvent)
            .WithMany(activityEvent => activityEvent.Animals)
            .HasForeignKey(association => association.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(association => association.Animal)
            .WithMany(animal => animal.ActivityEvents)
            .HasForeignKey(association => association.AnimalId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(association => association.AnimalId)
            .HasDatabaseName("IX_ActivityEventAnimals_AnimalId");

        builder.HasIndex(association => new { association.AnimalId, association.ActivityEventId })
            .HasDatabaseName("IX_ActivityEventAnimals_AnimalId_ActivityEventId");
    }
}
