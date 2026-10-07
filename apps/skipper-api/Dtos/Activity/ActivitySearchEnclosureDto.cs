using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.Activity;

/// <summary>
/// Enclosure associated with an activity search result.
/// </summary>
public class ActivitySearchEnclosureDto
{
    public int EnclosureId { get; set; }

    public required string EnclosureName { get; set; }

    public ActivityEventEnclosureRelationshipType RelationshipType { get; set; }
}
