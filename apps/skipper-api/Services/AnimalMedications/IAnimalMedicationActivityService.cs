using skipper_api.Dtos.AnimalMedications;

namespace skipper_api.Services.AnimalMedications;

public interface IAnimalMedicationActivityService
{
    Task<IReadOnlyList<AnimalMedicationActivityDto>?> GetByAnimalIdAsync(int animalId, CancellationToken cancellationToken = default);

    Task<AnimalMedicationActivityDto?> GetByIdAsync(int animalId, long activityId, CancellationToken cancellationToken = default);

    Task<CreateAnimalMedicationActivityResult> CreateAsync(int animalId, CreateAnimalMedicationActivityDto request, CancellationToken cancellationToken = default);

    Task<UpdateAnimalMedicationActivityResult> UpdateAsync(int animalId, long activityId, UpdateAnimalMedicationActivityDto request, CancellationToken cancellationToken = default);

    Task<DeleteAnimalMedicationActivityResult> DeleteAsync(int animalId, long activityId, CancellationToken cancellationToken = default);
}
