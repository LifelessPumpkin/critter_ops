using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

public class EnclosureWaterChangeActivityConfiguration : IEntityTypeConfiguration<EnclosureWaterChangeActivity>
{
    public void Configure(EntityTypeBuilder<EnclosureWaterChangeActivity> builder)
    {
        builder.ToTable("EnclosureWaterChangeActivities");
        builder.HasKey(waterChange => waterChange.ActivityEventId);
        builder.Property(waterChange => waterChange.WaterChangePercent).HasPrecision(5, 2);
        builder.HasOne(waterChange => waterChange.ActivityEvent)
            .WithOne(activityEvent => activityEvent.EnclosureWaterChange)
            .HasForeignKey<EnclosureWaterChangeActivity>(waterChange => waterChange.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
