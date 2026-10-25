using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core mapping for structured animal movement activity details.
/// </summary>
public class AnimalMovementActivityConfiguration : IEntityTypeConfiguration<AnimalMovementActivity>
{
    public void Configure(EntityTypeBuilder<AnimalMovementActivity> builder)
    {
        builder.ToTable("AnimalMovementActivities");

        builder.HasKey(movement => movement.ActivityEventId);

        builder.Property(movement => movement.Reason)
            .HasMaxLength(100);

        builder.HasOne(movement => movement.ActivityEvent)
            .WithOne(activityEvent => activityEvent.AnimalMovement)
            .HasForeignKey<AnimalMovementActivity>(movement => movement.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(movement => movement.FromEnclosure)
            .WithMany()
            .HasForeignKey(movement => movement.FromEnclosureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(movement => movement.ToEnclosure)
            .WithMany()
            .HasForeignKey(movement => movement.ToEnclosureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(movement => movement.FromEnclosureId)
            .HasDatabaseName("IX_AnimalMovementActivities_FromEnclosureId");

        builder.HasIndex(movement => movement.ToEnclosureId)
            .HasDatabaseName("IX_AnimalMovementActivities_ToEnclosureId");
    }
}
