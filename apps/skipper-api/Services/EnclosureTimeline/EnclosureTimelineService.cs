using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.EnclosureTimeline;
using skipper_api.Dtos.EnclosureTimeline;

namespace skipper_api.Services.EnclosureTimeline;

public class EnclosureTimelineService : IEnclosureTimelineService
{
    private readonly ProfessorDbContext _dbContext;

    public EnclosureTimelineService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EnclosureTimelineEventDto>> GetByEnclosureIdAsync(
        int enclosureId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EnclosureTimelineEvents
            .AsNoTracking()
            .Where(timelineEvent => timelineEvent.EnclosureId == enclosureId)
            .OrderByDescending(timelineEvent => timelineEvent.OccurredAt)
            .Select(timelineEvent => ToDto(timelineEvent))
            .ToListAsync(cancellationToken);
    }

    public async Task<EnclosureTimelineEventDto?> GetByIdAsync(
        int enclosureId,
        long eventId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.EnclosureTimelineEvents
            .AsNoTracking()
            .Where(timelineEvent => timelineEvent.Id == eventId && timelineEvent.EnclosureId == enclosureId)
            .SingleOrDefaultAsync(cancellationToken);

        return timelineEvent is null
            ? null
            : ToDto(timelineEvent);
    }

    public async Task<CreateTimelineEventResult> CreateAsync(
        int enclosureId,
        CreateEnclosureTimelineEventDto request,
        CancellationToken cancellationToken = default)
    {
        var enclosureExists = await _dbContext.Enclosures
            .AsNoTracking()
            .AnyAsync(enclosure => enclosure.Id == enclosureId, cancellationToken);

        if (!enclosureExists)
        {
            return CreateTimelineEventResult.EnclosureNotFound();
        }

        var now = DateTime.UtcNow;
        var timelineEvent = new EnclosureTimelineEvent
        {
            EnclosureId = enclosureId,
            EventType = request.EventType!.Value,
            OccurredAt = request.OccurredAt!.Value,
            Title = request.Title!,
            Description = request.Description,
            PerformedBy = request.PerformedBy,
            SourceReferenceId = request.SourceReferenceId,
            SourceType = request.SourceType,
            Metadata = ToJsonDocument(request.Metadata),
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.EnclosureTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateTimelineEventResult.Created(ToDto(timelineEvent));
    }

    public async Task<UpdateTimelineEventResult> UpdateAsync(
        int enclosureId,
        long eventId,
        UpdateEnclosureTimelineEventDto request,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.EnclosureTimelineEvents
            .SingleOrDefaultAsync(
                timelineEvent => timelineEvent.Id == eventId && timelineEvent.EnclosureId == enclosureId,
                cancellationToken);

        if (timelineEvent is null)
        {
            return UpdateTimelineEventResult.NotFound();
        }

        timelineEvent.EventType = request.EventType!.Value;
        timelineEvent.OccurredAt = request.OccurredAt!.Value;
        timelineEvent.Title = request.Title!;
        timelineEvent.Description = request.Description;
        timelineEvent.PerformedBy = request.PerformedBy;
        timelineEvent.Metadata = ToJsonDocument(request.Metadata);
        timelineEvent.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateTimelineEventResult.Updated(ToDto(timelineEvent));
    }

    public async Task<DeleteTimelineEventResult> DeleteAsync(
        int enclosureId,
        long eventId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.EnclosureTimelineEvents
            .SingleOrDefaultAsync(
                timelineEvent => timelineEvent.Id == eventId && timelineEvent.EnclosureId == enclosureId,
                cancellationToken);

        if (timelineEvent is null)
        {
            return DeleteTimelineEventResult.NotFound;
        }

        _dbContext.EnclosureTimelineEvents.Remove(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteTimelineEventResult.Deleted;
    }

    private static JsonDocument? ToJsonDocument(JsonElement? metadata)
    {
        return metadata.HasValue
            ? JsonDocument.Parse(metadata.Value.GetRawText())
            : null;
    }

    private static EnclosureTimelineEventDto ToDto(EnclosureTimelineEvent timelineEvent)
    {
        return new EnclosureTimelineEventDto
        {
            Id = timelineEvent.Id,
            EnclosureId = timelineEvent.EnclosureId,
            EventType = timelineEvent.EventType,
            OccurredAt = timelineEvent.OccurredAt,
            Title = timelineEvent.Title,
            Description = timelineEvent.Description,
            PerformedBy = timelineEvent.PerformedBy,
            SourceReferenceId = timelineEvent.SourceReferenceId,
            SourceType = timelineEvent.SourceType,
            Metadata = timelineEvent.Metadata?.RootElement.Clone(),
            CreatedAt = timelineEvent.CreatedAt,
            UpdatedAt = timelineEvent.UpdatedAt,
        };
    }
}
