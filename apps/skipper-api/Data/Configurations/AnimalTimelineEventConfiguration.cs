using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.AnimalTimeline;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core table mapping and column configuration for animal timeline events.
/// </summary>
public class AnimalTimelineEventConfiguration : IEntityTypeConfiguration<AnimalTimelineEvent>
{
    public void Configure(EntityTypeBuilder<AnimalTimelineEvent> builder)
    {
        builder.ToTable("AnimalTimelineEvents");

        // Primary key
        builder.HasKey(timelineEvent => timelineEvent.Id);
        builder.Property(timelineEvent => timelineEvent.Id).ValueGeneratedOnAdd();

        builder.Property(timelineEvent => timelineEvent.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(timelineEvent => timelineEvent.OccurredAt)
            .IsRequired();

        builder.Property(timelineEvent => timelineEvent.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(timelineEvent => timelineEvent.Description)
            .HasColumnType("text");

        builder.Property(timelineEvent => timelineEvent.PerformedBy)
            .HasMaxLength(100);

        builder.Property(timelineEvent => timelineEvent.SourceType)
            .HasMaxLength(100);

        builder.Property(timelineEvent => timelineEvent.Metadata)
            .HasColumnType("jsonb");

        builder.Property(timelineEvent => timelineEvent.CreatedAt).IsRequired();
        builder.Property(timelineEvent => timelineEvent.UpdatedAt).IsRequired();

        builder.HasOne(timelineEvent => timelineEvent.Animal)
            .WithMany(animal => animal.TimelineEvents)
            .HasForeignKey(timelineEvent => timelineEvent.AnimalId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(timelineEvent => timelineEvent.Enclosure)
            .WithMany(enclosure => enclosure.AnimalTimelineEvents)
            .HasForeignKey(timelineEvent => timelineEvent.EnclosureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(timelineEvent => timelineEvent.AnimalId)
            .HasDatabaseName("IX_AnimalTimelineEvents_AnimalId");

        builder.HasIndex(timelineEvent => timelineEvent.EnclosureId)
            .HasDatabaseName("IX_AnimalTimelineEvents_EnclosureId");

        builder.HasIndex(timelineEvent => timelineEvent.OccurredAt)
            .HasDatabaseName("IX_AnimalTimelineEvents_OccurredAt");

        builder.HasIndex(timelineEvent => timelineEvent.EventType)
            .HasDatabaseName("IX_AnimalTimelineEvents_EventType");

        builder.HasIndex(timelineEvent => new { timelineEvent.AnimalId, timelineEvent.OccurredAt, timelineEvent.Id })
            .HasDatabaseName("IX_AnimalTimelineEvents_AnimalId_OccurredAt_Id");
    }
}
