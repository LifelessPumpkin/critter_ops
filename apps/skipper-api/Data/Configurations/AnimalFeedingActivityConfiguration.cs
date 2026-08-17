using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core mapping for structured animal feeding activity details.
/// </summary>
public class AnimalFeedingActivityConfiguration : IEntityTypeConfiguration<AnimalFeedingActivity>
{
    public void Configure(EntityTypeBuilder<AnimalFeedingActivity> builder)
    {
        builder.ToTable("AnimalFeedingActivities");

        builder.HasKey(feeding => feeding.ActivityEventId);

        builder.Property(feeding => feeding.Food)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(feeding => feeding.Quantity)
            .IsRequired()
            .HasPrecision(12, 4);

        builder.Property(feeding => feeding.Unit)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(feeding => feeding.Result)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(feeding => feeding.ActivityEvent)
            .WithOne(activityEvent => activityEvent.AnimalFeeding)
            .HasForeignKey<AnimalFeedingActivity>(feeding => feeding.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(feeding => feeding.Food)
            .HasDatabaseName("IX_AnimalFeedingActivities_Food");

        builder.HasIndex(feeding => feeding.Result)
            .HasDatabaseName("IX_AnimalFeedingActivities_Result");
    }
}
