using skipper_api.Dtos.AnimalTimeline;

namespace skipper_api.Services.AnimalTimeline;

public interface IAnimalTimelineService
{
    Task<IReadOnlyList<AnimalTimelineEventDto>?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default);

    Task<AnimalTimelineEventDto?> GetByIdAsync(
        int animalId,
        long eventId,
        CancellationToken cancellationToken = default);

    Task<CreateAnimalTimelineEventResult> CreateAsync(
        int animalId,
        CreateAnimalTimelineEventDto request,
        CancellationToken cancellationToken = default);

    Task<UpdateAnimalTimelineEventResult> UpdateAsync(
        int animalId,
        long eventId,
        UpdateAnimalTimelineEventDto request,
        CancellationToken cancellationToken = default);

    Task<DeleteAnimalTimelineEventResult> DeleteAsync(
        int animalId,
        long eventId,
        CancellationToken cancellationToken = default);
}
