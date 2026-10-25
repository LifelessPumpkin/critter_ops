using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core mapping for structured enclosure cleaning activity details.
/// </summary>
public class EnclosureCleaningActivityConfiguration : IEntityTypeConfiguration<EnclosureCleaningActivity>
{
    public void Configure(EntityTypeBuilder<EnclosureCleaningActivity> builder)
    {
        builder.ToTable("EnclosureCleaningActivities");

        builder.HasKey(cleaning => cleaning.ActivityEventId);

        builder.Property(cleaning => cleaning.CleaningType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(cleaning => cleaning.WaterChangePercent)
            .HasPrecision(5, 2);

        builder.Property(cleaning => cleaning.SubstrateChanged)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(cleaning => cleaning.EquipmentCleaned)
            .HasMaxLength(250);

        builder.HasOne(cleaning => cleaning.ActivityEvent)
            .WithOne(activityEvent => activityEvent.EnclosureCleaning)
            .HasForeignKey<EnclosureCleaningActivity>(cleaning => cleaning.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(cleaning => cleaning.CleaningType)
            .HasDatabaseName("IX_EnclosureCleaningActivities_CleaningType");
    }
}
