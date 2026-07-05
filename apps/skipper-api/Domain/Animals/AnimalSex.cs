namespace skipper_api.Domain.Animals;

/// <summary>
/// Represents animal sex where known or applicable.
/// </summary>
public enum AnimalSex
{
    /// <summary>Male animal.</summary>
    Male,

    /// <summary>Female animal.</summary>
    Female,

    /// <summary>Sex is unknown.</summary>
    Unknown,

    /// <summary>Hermaphroditic animal.</summary>
    Hermaphrodite,

    /// <summary>Mixed-sex group or colony represented as a single animal record.</summary>
    Mixed,

    /// <summary>Sex is not applicable.</summary>
    NotApplicable,
}
