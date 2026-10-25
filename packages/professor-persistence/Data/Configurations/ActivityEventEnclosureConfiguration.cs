using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core mapping for activity-to-enclosure associations.
/// </summary>
public class ActivityEventEnclosureConfiguration : IEntityTypeConfiguration<ActivityEventEnclosure>
{
    public void Configure(EntityTypeBuilder<ActivityEventEnclosure> builder)
    {
        builder.ToTable("ActivityEventEnclosures");

        builder.HasKey(association => new
        {
            association.ActivityEventId,
            association.EnclosureId,
            association.RelationshipType,
        });

        builder.Property(association => association.RelationshipType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(association => association.ActivityEvent)
            .WithMany(activityEvent => activityEvent.Enclosures)
            .HasForeignKey(association => association.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(association => association.Enclosure)
            .WithMany(enclosure => enclosure.ActivityEvents)
            .HasForeignKey(association => association.EnclosureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(association => association.EnclosureId)
            .HasDatabaseName("IX_ActivityEventEnclosures_EnclosureId");

        builder.HasIndex(association => new { association.EnclosureId, association.ActivityEventId })
            .HasDatabaseName("IX_ActivityEventEnclosures_EnclosureId_ActivityEventId");
    }
}
