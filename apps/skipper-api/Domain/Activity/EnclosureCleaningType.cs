namespace skipper_api.Domain.Activity;

/// <summary>
/// Type of enclosure cleaning activity performed.
/// </summary>
public enum EnclosureCleaningType
{
    SpotClean,
    PartialClean,
    FullClean,
    DeepClean,
    Disinfection,
    WaterChange,
    SubstrateChange,
    Other,
}
