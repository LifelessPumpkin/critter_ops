namespace skipper_api.Domain.Enclosures;

/// <summary>
/// Represents the broad category of a physical enclosure.
/// </summary>
public enum EnclosureType
{
    /// <summary>Glass or acrylic tank used for aquatic or terrestrial animals.</summary>
    Aquarium,

    /// <summary>Outdoor or indoor body of water used for fish, amphibians, or water features.</summary>
    Pond,

    /// <summary>Enclosed habitat designed for reptiles or arid/semi-arid species.</summary>
    Terrarium,

    /// <summary>Naturalistic, planted habitat with living substrate and vegetation.</summary>
    Vivarium,

    /// <summary>Hybrid enclosure supporting both aquatic and terrestrial environments.</summary>
    Paludarium,

    /// <summary>Large enclosed space designed for birds to fly freely.</summary>
    Aviary,

    /// <summary>A run or enclosure for dogs or other medium-to-large mammals.</summary>
    Kennel,

    /// <summary>Wire or barred enclosure for birds, small mammals, or reptiles.</summary>
    Cage,

    /// <summary>Portable enclosure used for transport or temporary housing.</summary>
    Crate,

    /// <summary>Small hutch or pen typically used for rabbits or guinea pigs.</summary>
    Hutch,

    /// <summary>Managed hive structure for honey bees or other bee species.</summary>
    Beehive,

    /// <summary>Large enclosed structure used for horses, donkeys, or livestock.</summary>
    Stable,

    /// <summary>Open-top plastic tub used for turtles, frogs, or small aquatic setups.</summary>
    Tub,

    /// <summary>Shelving system used to house multiple small enclosures, common in reptile breeding.</summary>
    Rack,

    /// <summary>Fenced or enclosed outdoor area allowing animals to roam.</summary>
    OutdoorRun,

    /// <summary>A type not covered by current values. Use until a dedicated value is added.</summary>
    Other,
}
