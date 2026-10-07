using skipper_api.Dtos.EnclosureCleanings;

namespace skipper_api.Services.EnclosureCleanings;

public interface IEnclosureCleaningActivityService
{
    Task<IReadOnlyList<EnclosureCleaningActivityDto>?> GetByEnclosureIdAsync(
        int enclosureId,
        CancellationToken cancellationToken = default);

    Task<EnclosureCleaningActivityDto?> GetByIdAsync(
        int enclosureId,
        long activityId,
        CancellationToken cancellationToken = default);

    Task<CreateEnclosureCleaningActivityResult> CreateAsync(
        int enclosureId,
        CreateEnclosureCleaningActivityDto request,
        CancellationToken cancellationToken = default);

    Task<UpdateEnclosureCleaningActivityResult> UpdateAsync(
        int enclosureId,
        long activityId,
        UpdateEnclosureCleaningActivityDto request,
        CancellationToken cancellationToken = default);

    Task<DeleteEnclosureCleaningActivityResult> DeleteAsync(
        int enclosureId,
        long activityId,
        CancellationToken cancellationToken = default);
}
