using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core table mapping and column configuration for shared activity ledger events.
/// </summary>
public class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> builder)
    {
        builder.ToTable("ActivityEvents");

        builder.HasKey(activityEvent => activityEvent.Id);
        builder.Property(activityEvent => activityEvent.Id).ValueGeneratedOnAdd();

        builder.Property(activityEvent => activityEvent.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(activityEvent => activityEvent.OccurredAt)
            .IsRequired();

        builder.Property(activityEvent => activityEvent.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(activityEvent => activityEvent.Notes)
            .HasColumnType("text");

        builder.Property(activityEvent => activityEvent.PerformedBy)
            .HasMaxLength(100);

        builder.Property(activityEvent => activityEvent.SourceType)
            .HasMaxLength(100);

        builder.Property(activityEvent => activityEvent.Metadata)
            .HasColumnType("jsonb");

        builder.Property(activityEvent => activityEvent.CreatedAt).IsRequired();
        builder.Property(activityEvent => activityEvent.UpdatedAt).IsRequired();

        builder.HasIndex(activityEvent => activityEvent.OccurredAt)
            .HasDatabaseName("IX_ActivityEvents_OccurredAt");

        builder.HasIndex(activityEvent => activityEvent.EventType)
            .HasDatabaseName("IX_ActivityEvents_EventType");
    }
}
