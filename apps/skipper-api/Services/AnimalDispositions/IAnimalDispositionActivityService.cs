using skipper_api.Dtos.AnimalDispositions;

namespace skipper_api.Services.AnimalDispositions;

public interface IAnimalDispositionActivityService
{
    Task<IReadOnlyList<AnimalDispositionActivityDto>?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default);

    Task<AnimalDispositionActivityDto?> GetByIdAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default);

    Task<CreateAnimalDispositionActivityResult> CreateAsync(
        int animalId,
        CreateAnimalDispositionActivityDto request,
        CancellationToken cancellationToken = default);

    Task<UpdateAnimalDispositionActivityResult> UpdateAsync(
        int animalId,
        long activityId,
        UpdateAnimalDispositionActivityDto request,
        CancellationToken cancellationToken = default);

    Task<DeleteAnimalDispositionActivityResult> DeleteAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default);
}
