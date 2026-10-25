using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskEntity = skipper_api.Domain.Tasks.Task;

namespace skipper_api.Data.Configurations;

/// <summary>
/// EF Core table mapping and column configuration for husbandry tasks.
/// </summary>
public class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedOnAdd();

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(task => task.Description)
            .HasColumnType("text");

        builder.Property(task => task.TaskType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(task => task.DueAt)
            .IsRequired();

        builder.Property(task => task.RecurrenceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(task => task.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(task => task.CompletionNotes)
            .HasColumnType("text");

        builder.Property(task => task.CompletedBy)
            .HasMaxLength(100);

        builder.Property(task => task.CreatedAt).IsRequired();
        builder.Property(task => task.UpdatedAt).IsRequired();

        builder.HasOne(task => task.Animal)
            .WithMany()
            .HasForeignKey(task => task.AnimalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.Enclosure)
            .WithMany()
            .HasForeignKey(task => task.EnclosureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(task => task.DueAt)
            .HasDatabaseName("IX_Tasks_DueAt");

        builder.HasIndex(task => task.IsCompleted)
            .HasDatabaseName("IX_Tasks_IsCompleted");

        builder.HasIndex(task => new { task.IsCompleted, task.DueAt })
            .HasDatabaseName("IX_Tasks_IsCompleted_DueAt");

        builder.HasIndex(task => task.AnimalId)
            .HasDatabaseName("IX_Tasks_AnimalId");

        builder.HasIndex(task => task.EnclosureId)
            .HasDatabaseName("IX_Tasks_EnclosureId");
    }
}
