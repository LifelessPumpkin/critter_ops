using skipper_api.Domain.Animals;
using skipper_api.Domain.Enclosures;

namespace skipper_api.Domain.Tasks;

/// <summary>
/// Scheduled husbandry work item or generated occurrence.
/// </summary>
public class Task
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Short human-readable task title.</summary>
    public required string Title { get; set; }

    /// <summary>Optional details for the work to perform.</summary>
    public string? Description { get; set; }

    /// <summary>Broad husbandry task category.</summary>
    public required TaskType TaskType { get; set; }

    /// <summary>UTC date/time when this task occurrence is due.</summary>
    public required DateTime DueAt { get; set; }

    /// <summary>Simple recurrence cadence used to create the next occurrence.</summary>
    public required RecurrenceType RecurrenceType { get; set; }

    /// <summary>Interval used by recurring schedules. Custom recurrence treats this as days.</summary>
    public int? RecurrenceInterval { get; set; }

    /// <summary>Whether this task occurrence has been completed.</summary>
    public required bool IsCompleted { get; set; }

    /// <summary>UTC date/time when completion was recorded.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Optional notes captured when the task is completed.</summary>
    public string? CompletionNotes { get; set; }

    /// <summary>Human-readable actor name until user accounts are introduced.</summary>
    public string? CompletedBy { get; set; }

    /// <summary>Optional animal this task relates to.</summary>
    public int? AnimalId { get; set; }

    /// <summary>Related animal, when present.</summary>
    public Animal? Animal { get; set; }

    /// <summary>Optional enclosure this task relates to.</summary>
    public int? EnclosureId { get; set; }

    /// <summary>Related enclosure, when present.</summary>
    public Enclosure? Enclosure { get; set; }

    /// <summary>UTC timestamp when this record was created.</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when this record was last modified.</summary>
    public required DateTime UpdatedAt { get; set; }
}
