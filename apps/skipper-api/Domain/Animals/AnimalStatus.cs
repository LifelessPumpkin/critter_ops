namespace skipper_api.Domain.Animals;

/// <summary>
/// Represents the current state of an animal.
/// </summary>
public enum AnimalStatus
{
    /// <summary>The animal is active in the collection.</summary>
    Active,

    /// <summary>The animal is in quarantine.</summary>
    Quarantined,

    /// <summary>The animal is under medical observation or treatment.</summary>
    Medical,

    /// <summary>The animal is in a breeding program or pairing.</summary>
    Breeding,

    /// <summary>The animal is held and not available for transfer or sale.</summary>
    OnHold,

    /// <summary>The animal has been transferred out of the collection.</summary>
    Transferred,

    /// <summary>The animal has been sold.</summary>
    Sold,

    /// <summary>The animal is deceased.</summary>
    Deceased,

    /// <summary>The animal has been released.</summary>
    Released,

    /// <summary>The animal is inactive in the collection.</summary>
    Inactive,
}
