using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core mapping for structured animal medication activity details.
/// </summary>
public class AnimalMedicationActivityConfiguration : IEntityTypeConfiguration<AnimalMedicationActivity>
{
    public void Configure(EntityTypeBuilder<AnimalMedicationActivity> builder)
    {
        builder.ToTable("AnimalMedicationActivities");

        builder.HasKey(medication => medication.ActivityEventId);

        builder.Property(medication => medication.MedicationName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(medication => medication.Dose)
            .IsRequired()
            .HasPrecision(12, 4);

        builder.Property(medication => medication.DoseUnit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(medication => medication.Route)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(medication => medication.Result)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(medication => medication.ActivityEvent)
            .WithOne(activityEvent => activityEvent.AnimalMedication)
            .HasForeignKey<AnimalMedicationActivity>(medication => medication.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(medication => medication.MedicationName)
            .HasDatabaseName("IX_AnimalMedicationActivities_MedicationName");

        builder.HasIndex(medication => medication.Route)
            .HasDatabaseName("IX_AnimalMedicationActivities_Route");
    }
}
