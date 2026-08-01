using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.EnclosureTimeline;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core table mapping and column configuration for enclosure timeline events.
/// </summary>
public class EnclosureTimelineEventConfiguration : IEntityTypeConfiguration<EnclosureTimelineEvent>
{
    public void Configure(EntityTypeBuilder<EnclosureTimelineEvent> builder)
    {
        builder.ToTable("EnclosureTimelineEvents");

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

        builder.HasOne(timelineEvent => timelineEvent.Enclosure)
            .WithMany(enclosure => enclosure.TimelineEvents)
            .HasForeignKey(timelineEvent => timelineEvent.EnclosureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(timelineEvent => timelineEvent.EnclosureId)
            .HasDatabaseName("IX_EnclosureTimelineEvents_EnclosureId");

        builder.HasIndex(timelineEvent => timelineEvent.OccurredAt)
            .HasDatabaseName("IX_EnclosureTimelineEvents_OccurredAt");

        builder.HasIndex(timelineEvent => timelineEvent.EventType)
            .HasDatabaseName("IX_EnclosureTimelineEvents_EventType");

        builder.HasIndex(timelineEvent => new { timelineEvent.EnclosureId, timelineEvent.OccurredAt })
            .HasDatabaseName("IX_EnclosureTimelineEvents_EnclosureId_OccurredAt");
    }
}
