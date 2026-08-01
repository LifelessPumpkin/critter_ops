using skipper_api.Domain.Animals;
using skipper_api.Domain.EnclosureTimeline;

namespace skipper_api.Domain.Enclosures;

/// <summary>
/// Represents a physical habitat that houses one or more animals.
/// This is the shared base enclosure model applicable to all enclosure types.
/// Species-specific detail and operational history are modeled in separate related entities.
/// </summary>
public class Enclosure
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable name for the enclosure.
    /// Examples: "Living Room 40 Gallon Breeder", "Backyard Rabbit Hutch", "Reptile Room Terrarium 1".
    /// </summary>
    public required string Name { get; set; }

    /// <summary>Broad category of the enclosure.</summary>
    public required EnclosureType Type { get; set; }

    /// <summary>
    /// Descriptive free-text location of the enclosure within the facility or property.
    /// Examples: "Living Room", "Fish Room", "Reptile Room", "Backyard", "Shed", "Garage".
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// Optional human-readable size description.
    /// Examples: "10 Gallon", "40 Gallon Breeder", "6 ft x 3 ft Kennel", "1000 Gallon Pond".
    /// </summary>
    public string? SizeLabel { get; set; }

    /// <summary>Physical length of the enclosure.</summary>
    public decimal? Length { get; set; }

    /// <summary>Physical width of the enclosure.</summary>
    public decimal? Width { get; set; }

    /// <summary>Physical height of the enclosure.</summary>
    public decimal? Height { get; set; }

    /// <summary>
    /// Unit of measurement for physical dimensions.
    /// Examples: "Inches", "Feet", "Centimeters", "Meters".
    /// </summary>
    public string? DimensionUnit { get; set; }

    /// <summary>
    /// Fluid or volume capacity of the enclosure. Useful for aquariums, ponds, and tubs.
    /// </summary>
    public decimal? Volume { get; set; }

    /// <summary>
    /// Unit of measurement for volume.
    /// Examples: "Gallons", "Liters".
    /// </summary>
    public string? VolumeUnit { get; set; }

    /// <summary>
    /// Primary construction material.
    /// Examples: "Glass", "Acrylic", "Wire", "Plastic", "Wood", "PVC", "Mesh", "Concrete", "Soft-sided fabric".
    /// </summary>
    public string? Material { get; set; }

    /// <summary>Maximum intended number of animals the enclosure is designed to house.</summary>
    public int? MaxAnimalCapacity { get; set; }

    /// <summary>Indicates whether and how the enclosure can be relocated.</summary>
    public required EnclosureMobility Mobility { get; set; }

    /// <summary>Current operational state of the enclosure.</summary>
    public required EnclosureStatus Status { get; set; }

    /// <summary>
    /// General safety and containment assessment.
    /// Examples: "Low", "Medium", "High", "Critical".
    /// </summary>
    public string? SafetyRating { get; set; }

    /// <summary>Free-form notes about the enclosure.</summary>
    public string? Notes { get; set; }

    /// <summary>Animals currently associated with this enclosure.</summary>
    public ICollection<Animal> Animals { get; set; } = [];

    /// <summary>Historical activity associated with this enclosure.</summary>
    public ICollection<EnclosureTimelineEvent> TimelineEvents { get; set; } = [];

    /// <summary>UTC timestamp when this record was created.</summary>
    public required DateTime CreatedDate { get; set; }

    /// <summary>UTC timestamp when this record was last modified.</summary>
    public required DateTime UpdatedDate { get; set; }
}
