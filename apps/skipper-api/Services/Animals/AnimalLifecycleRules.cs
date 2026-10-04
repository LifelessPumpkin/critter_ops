using skipper_api.Domain.Animals;

namespace skipper_api.Services.Animals;

/// <summary>Single source of truth for lifecycle groups derived from animal status.</summary>
public static class AnimalLifecycleRules
{
    public static readonly AnimalStatus[] InCareStatuses =
    [
        AnimalStatus.Active,
        AnimalStatus.Quarantined,
        AnimalStatus.Medical,
        AnimalStatus.Breeding,
        AnimalStatus.OnHold,
    ];

    public static readonly AnimalStatus[] OutOfCareStatuses =
    [
        AnimalStatus.Transferred,
        AnimalStatus.Sold,
        AnimalStatus.Deceased,
        AnimalStatus.Released,
        AnimalStatus.Surrendered,
        AnimalStatus.Inactive,
    ];
}
