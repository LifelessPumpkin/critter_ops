using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Tasks;

namespace skipper_api.Dtos.Tasks;

/// <summary>
/// API request shape for creating a husbandry task.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateTaskRequestDto : IValidatableObject
{
    [Required]
    [MaxLength(150)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public TaskType? TaskType { get; set; }

    [Required]
    public DateTime? DueAt { get; set; }

    [Required]
    public RecurrenceType? RecurrenceType { get; set; }

    [Range(1, int.MaxValue)]
    public int? RecurrenceInterval { get; set; }

    public int? AnimalId { get; set; }

    public int? EnclosureId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RecurrenceType == Domain.Tasks.RecurrenceType.Custom && RecurrenceInterval is null)
        {
            yield return new ValidationResult(
                "RecurrenceInterval is required for custom recurrence.",
                [nameof(RecurrenceInterval)]);
        }

        if (RecurrenceType == Domain.Tasks.RecurrenceType.None && RecurrenceInterval is not null)
        {
            yield return new ValidationResult(
                "RecurrenceInterval is only supported for recurring tasks.",
                [nameof(RecurrenceInterval)]);
        }
    }
}
