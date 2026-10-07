using skipper_api.Dtos.AnimalTreatments;

namespace skipper_api.Services.AnimalTreatments;

public interface IAnimalTreatmentActivityService
{
    Task<IReadOnlyList<AnimalTreatmentActivityDto>?> GetByAnimalIdAsync(int animalId, CancellationToken cancellationToken = default);

    Task<AnimalTreatmentActivityDto?> GetByIdAsync(int animalId, long activityId, CancellationToken cancellationToken = default);

    Task<CreateAnimalTreatmentActivityResult> CreateAsync(int animalId, CreateAnimalTreatmentActivityDto request, CancellationToken cancellationToken = default);

    Task<UpdateAnimalTreatmentActivityResult> UpdateAsync(int animalId, long activityId, UpdateAnimalTreatmentActivityDto request, CancellationToken cancellationToken = default);

    Task<DeleteAnimalTreatmentActivityResult> DeleteAsync(int animalId, long activityId, CancellationToken cancellationToken = default);
}
