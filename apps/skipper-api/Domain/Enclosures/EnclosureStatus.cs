namespace skipper_api.Domain.Enclosures;

/// <summary>
/// Represents the current operational state of an enclosure.
/// </summary>
public enum EnclosureStatus
{
    /// <summary>The enclosure is in use and operating normally.</summary>
    Active,

    /// <summary>The enclosure has no current animal occupants.</summary>
    Empty,

    /// <summary>The enclosure is not currently in use and not planned for immediate use.</summary>
    Inactive,

    /// <summary>The enclosure is being repaired, modified, or is otherwise out of service.</summary>
    UnderMaintenance,

    /// <summary>The enclosure requires cleaning before it can be used.</summary>
    CleaningRequired,

    /// <summary>The enclosure is isolated to contain or prevent spread of disease.</summary>
    Quarantine,

    /// <summary>The enclosure is held for a planned future use.</summary>
    Reserved,

    /// <summary>The enclosure is permanently decommissioned.</summary>
    Retired,
}
