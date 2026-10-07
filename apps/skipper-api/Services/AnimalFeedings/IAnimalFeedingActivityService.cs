using skipper_api.Dtos.AnimalFeedings;

namespace skipper_api.Services.AnimalFeedings;

public interface IAnimalFeedingActivityService
{
    Task<IReadOnlyList<AnimalFeedingActivityDto>?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default);

    Task<AnimalFeedingActivityDto?> GetByIdAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default);

    Task<CreateAnimalFeedingActivityResult> CreateAsync(
        int animalId,
        CreateAnimalFeedingActivityDto request,
        CancellationToken cancellationToken = default);

    Task<UpdateAnimalFeedingActivityResult> UpdateAsync(
        int animalId,
        long activityId,
        UpdateAnimalFeedingActivityDto request,
        CancellationToken cancellationToken = default);

    Task<DeleteAnimalFeedingActivityResult> DeleteAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default);
}
