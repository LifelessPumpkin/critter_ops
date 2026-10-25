namespace skipper_api.Domain.Enclosures;

/// <summary>
/// Represents whether and how an enclosure can be relocated.
/// </summary>
public enum EnclosureMobility
{
    /// <summary>Permanently installed; not intended to be moved.</summary>
    Fixed,

    /// <summary>Can be relocated with effort (e.g. large tank on a stand).</summary>
    Movable,

    /// <summary>Lightweight and designed for frequent relocation (e.g. travel crate).</summary>
    Portable,

    /// <summary>Erected for short-term use and intended to be disassembled.</summary>
    Temporary,

    /// <summary>Permanently placed outdoors (e.g. in-ground pond, cemented aviary).</summary>
    OutdoorPermanent,
}
