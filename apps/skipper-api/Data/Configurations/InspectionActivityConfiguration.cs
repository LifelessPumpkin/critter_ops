using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

public class InspectionActivityConfiguration : IEntityTypeConfiguration<InspectionActivity>
{
    public void Configure(EntityTypeBuilder<InspectionActivity> builder)
    {
        builder.ToTable("InspectionActivities");
        builder.HasKey(inspection => inspection.ActivityEventId);
        builder.Property(inspection => inspection.Result)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
        builder.HasOne(inspection => inspection.ActivityEvent)
            .WithOne(activityEvent => activityEvent.Inspection)
            .HasForeignKey<InspectionActivity>(inspection => inspection.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.HasIndex(inspection => inspection.Result)
            .HasDatabaseName("IX_InspectionActivities_Result");
    }
}
