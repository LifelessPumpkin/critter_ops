using skipper_api.Dtos.AnimalMovements;

namespace skipper_api.Services.AnimalMovements;

public interface IAnimalMovementActivityService
{
    Task<IReadOnlyList<AnimalMovementActivityDto>?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default);

    Task<AnimalMovementActivityDto?> GetByIdAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default);

    Task<CreateAnimalMovementActivityResult> CreateAsync(
        int animalId,
        CreateAnimalMovementActivityDto request,
        CancellationToken cancellationToken = default);

    Task<UpdateAnimalMovementActivityResult> UpdateAsync(
        int animalId,
        long activityId,
        UpdateAnimalMovementActivityDto request,
        CancellationToken cancellationToken = default);

    Task<DeleteAnimalMovementActivityResult> DeleteAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default);
}
