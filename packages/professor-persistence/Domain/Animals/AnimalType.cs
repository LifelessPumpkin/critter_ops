namespace skipper_api.Domain.Animals;

/// <summary>
/// Represents a broad animal category.
/// </summary>
public enum AnimalType
{
    /// <summary>Mammal species.</summary>
    Mammal,

    /// <summary>Bird species.</summary>
    Avian,

    /// <summary>Operational grouping for reptiles and amphibians.</summary>
    Herptile,

    /// <summary>Aquatic species or species managed primarily in water.</summary>
    Aquatic,

    /// <summary>Invertebrate species.</summary>
    Invertebrate,

    /// <summary>Amphibian species.</summary>
    Amphibian,

    /// <summary>Reptile species.</summary>
    Reptile,

    /// <summary>Fish species.</summary>
    Fish,

    /// <summary>A type not covered by current values. Use until a dedicated value is added.</summary>
    Other,
}
