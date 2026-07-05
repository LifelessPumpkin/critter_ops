namespace skipper_api.Services.Enclosures;

/// <summary>
/// Result of an enclosure delete operation.
/// </summary>
public enum DeleteEnclosureResult
{
    Deleted,
    NotFound,
    HasAssignedAnimals,
}
