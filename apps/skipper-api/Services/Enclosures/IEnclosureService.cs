using skipper_api.Dtos.Enclosures;

namespace skipper_api.Services.Enclosures;

public interface IEnclosureService
{
    Task<IReadOnlyList<EnclosureResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<EnclosureResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<EnclosureResponseDto> CreateAsync(CreateEnclosureRequestDto request, CancellationToken cancellationToken = default);

    Task<EnclosureResponseDto?> UpdateAsync(int id, UpdateEnclosureRequestDto request, CancellationToken cancellationToken = default);

    Task<DeleteEnclosureResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
