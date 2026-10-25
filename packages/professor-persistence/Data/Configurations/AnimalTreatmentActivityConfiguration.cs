using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core mapping for structured animal treatment activity details.
/// </summary>
public class AnimalTreatmentActivityConfiguration : IEntityTypeConfiguration<AnimalTreatmentActivity>
{
    public void Configure(EntityTypeBuilder<AnimalTreatmentActivity> builder)
    {
        builder.ToTable("AnimalTreatmentActivities");

        builder.HasKey(treatment => treatment.ActivityEventId);

        builder.Property(treatment => treatment.TreatmentType)
            .HasMaxLength(100);

        builder.Property(treatment => treatment.TreatmentName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(treatment => treatment.Result)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(treatment => treatment.ActivityEvent)
            .WithOne(activityEvent => activityEvent.AnimalTreatment)
            .HasForeignKey<AnimalTreatmentActivity>(treatment => treatment.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(treatment => treatment.TreatmentType)
            .HasDatabaseName("IX_AnimalTreatmentActivities_TreatmentType");

        builder.HasIndex(treatment => treatment.TreatmentName)
            .HasDatabaseName("IX_AnimalTreatmentActivities_TreatmentName");
    }
}
