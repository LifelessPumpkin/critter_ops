using skipper_api.Domain.Tasks;

namespace skipper_api.Dtos.Tasks;

/// <summary>
/// API response shape for husbandry tasks.
/// </summary>
public class TaskResponseDto
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public TaskType TaskType { get; set; }

    public DateTime DueAt { get; set; }

    public RecurrenceType RecurrenceType { get; set; }

    public int? RecurrenceInterval { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CompletionNotes { get; set; }

    public string? CompletedBy { get; set; }

    public int? AnimalId { get; set; }

    public string? AnimalName { get; set; }

    public int? EnclosureId { get; set; }

    public string? EnclosureName { get; set; }

    public bool IsOverdue { get; set; }

    public bool IsDueToday { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
