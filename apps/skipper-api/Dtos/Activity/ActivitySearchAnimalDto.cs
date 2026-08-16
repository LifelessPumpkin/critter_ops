using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.Activity;

/// <summary>
/// Animal associated with an activity search result.
/// </summary>
public class ActivitySearchAnimalDto
{
    public int AnimalId { get; set; }

    public required string AnimalName { get; set; }

    public ActivityEventAnimalRelationshipType RelationshipType { get; set; }
}
