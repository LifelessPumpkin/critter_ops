namespace skipper_api.Domain.Tasks;

/// <summary>
/// Simple recurrence cadence for generated task occurrences.
/// </summary>
public enum RecurrenceType
{
    None,
    Daily,
    Weekly,
    Monthly,
    Custom,
}
