using skipper_api.Dtos.EnclosureTimeline;

namespace skipper_api.Services.EnclosureTimeline;

public interface IEnclosureTimelineService
{
    Task<IReadOnlyList<EnclosureTimelineEventDto>> GetByEnclosureIdAsync(
        int enclosureId,
        CancellationToken cancellationToken = default);

    Task<EnclosureTimelineEventDto?> GetByIdAsync(
        int enclosureId,
        long eventId,
        CancellationToken cancellationToken = default);

    Task<CreateTimelineEventResult> CreateAsync(
        int enclosureId,
        CreateEnclosureTimelineEventDto request,
        CancellationToken cancellationToken = default);

    Task<UpdateTimelineEventResult> UpdateAsync(
        int enclosureId,
        long eventId,
        UpdateEnclosureTimelineEventDto request,
        CancellationToken cancellationToken = default);

    Task<DeleteTimelineEventResult> DeleteAsync(
        int enclosureId,
        long eventId,
        CancellationToken cancellationToken = default);
}
