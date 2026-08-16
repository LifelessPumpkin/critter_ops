using skipper_api.Domain.Animals;

namespace skipper_api.Domain.Activity;

/// <summary>
/// Associates an activity ledger event with an animal.
/// </summary>
public class ActivityEventAnimal
{
    /// <summary>Activity event foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Activity event this association belongs to.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Animal foreign key.</summary>
    public int AnimalId { get; set; }

    /// <summary>Animal associated with the event.</summary>
    public Animal Animal { get; set; } = null!;

    /// <summary>How the animal relates to the event.</summary>
    public required ActivityEventAnimalRelationshipType RelationshipType { get; set; }
}
