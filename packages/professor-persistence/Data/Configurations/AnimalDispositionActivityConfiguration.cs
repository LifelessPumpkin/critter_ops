using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skipper_api.Domain.Activity;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core mapping for structured animal disposition activity details.
/// </summary>
public class AnimalDispositionActivityConfiguration : IEntityTypeConfiguration<AnimalDispositionActivity>
{
    public void Configure(EntityTypeBuilder<AnimalDispositionActivity> builder)
    {
        builder.ToTable("AnimalDispositionActivities");

        builder.HasKey(disposition => disposition.ActivityEventId);

        builder.Property(disposition => disposition.DispositionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(disposition => disposition.Reason)
            .HasMaxLength(150);

        builder.Property(disposition => disposition.RecipientOrDestination)
            .HasMaxLength(150);

        builder.HasOne(disposition => disposition.ActivityEvent)
            .WithOne(activityEvent => activityEvent.AnimalDisposition)
            .HasForeignKey<AnimalDispositionActivity>(disposition => disposition.ActivityEventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(disposition => disposition.DispositionType)
            .HasDatabaseName("IX_AnimalDispositionActivities_DispositionType");
    }
}
