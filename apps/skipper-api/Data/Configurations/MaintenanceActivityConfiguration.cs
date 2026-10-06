using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

public class MaintenanceActivityConfiguration : IEntityTypeConfiguration<MaintenanceActivity>
{
    public void Configure(EntityTypeBuilder<MaintenanceActivity> builder)
    {
        builder.ToTable("MaintenanceActivities");
        builder.HasKey(maintenance => maintenance.ActivityEventId);
        builder.Property(maintenance => maintenance.Description)
            .IsRequired()
            .HasColumnType("text");
        builder.HasOne(maintenance => maintenance.ActivityEvent)
            .WithOne(activityEvent => activityEvent.Maintenance)
            .HasForeignKey<MaintenanceActivity>(maintenance => maintenance.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
