using skipper_api.Domain.AnimalTimeline;
using skipper_api.Domain.Enclosures;

namespace skipper_api.Domain.Animals;

/// <summary>
/// Represents an animal housed in CritterOps.
/// This is the shared base animal model; medical, breeding, genetics, and measurement history
/// are modeled in separate related entities.
/// </summary>
public class Animal
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Required foreign key to the enclosure currently housing the animal.</summary>
    public int EnclosureId { get; set; }

    /// <summary>Parent enclosure where the animal is housed.</summary>
    public required Enclosure Enclosure { get; set; }

    /// <summary>Animal's given or display name.</summary>
    public required string Name { get; set; }

    /// <summary>Species name, such as Ball Python, Red Kangaroo, Cockatiel, or Axolotl.</summary>
    public required string Species { get; set; }

    /// <summary>Optional subspecies, morph, locality, or similar descriptor.</summary>
    public string? SubspeciesOrMorph { get; set; }

    /// <summary>Broad animal category.</summary>
    public required AnimalType AnimalType { get; set; }

    /// <summary>Current animal status.</summary>
    public required AnimalStatus Status { get; set; }

    /// <summary>Biological sex where known.</summary>
    public required AnimalSex Sex { get; set; }

    /// <summary>Free-form notes about the animal.</summary>
    public string? Notes { get; set; }

    /// <summary>Known or estimated birth date.</summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>Indicates whether the birth date is estimated.</summary>
    public required bool BirthDateIsEstimated { get; set; }

    /// <summary>Date the animal was acquired by the collection.</summary>
    public required DateOnly AcquiredDate { get; set; }

    /// <summary>Date the animal left the collection or died.</summary>
    public DateOnly? DispositionDate { get; set; }

    /// <summary>Reason the animal left the collection or became disposed.</summary>
    public string? DispositionReason { get; set; }

    /// <summary>Implanted microchip identifier, if applicable.</summary>
    public string? MicrochipNumber { get; set; }

    /// <summary>External tag, band, livestock, or other identifier.</summary>
    public string? TagIdentifier { get; set; }

    /// <summary>Where the animal was obtained from.</summary>
    public string? Source { get; set; }

    /// <summary>Historical activity associated with this animal.</summary>
    public ICollection<AnimalTimelineEvent> TimelineEvents { get; set; } = [];

    /// <summary>UTC timestamp when this record was created.</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when this record was last modified.</summary>
    public required DateTime UpdatedAt { get; set; }
}
