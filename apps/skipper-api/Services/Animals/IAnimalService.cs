using skipper_api.Dtos.Animals;

namespace skipper_api.Services.Animals;

public interface IAnimalService
{
    Task<IReadOnlyList<AnimalResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AnimalResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CreateAnimalResult> CreateAsync(CreateAnimalRequestDto request, CancellationToken cancellationToken = default);

    Task<UpdateAnimalResult> UpdateAsync(int id, UpdateAnimalRequestDto request, CancellationToken cancellationToken = default);

    Task<DeleteAnimalResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
