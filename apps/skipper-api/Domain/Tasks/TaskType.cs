namespace skipper_api.Domain.Tasks;

/// <summary>
/// Broad category for scheduled husbandry work.
/// </summary>
public enum TaskType
{
    Feeding,
    Cleaning,
    WaterChange,
    Medication,
    Inspection,
    Maintenance,
    General,
    Other,
}
