using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.AnimalTimeline;
using skipper_api.Dtos.AnimalTimeline;

namespace skipper_api.Services.AnimalTimeline;

public class AnimalTimelineService : IAnimalTimelineService
{
    private readonly ProfessorDbContext _dbContext;

    public AnimalTimelineService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalTimelineEventDto>?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default)
    {
        var animalExists = await _dbContext.Animals
            .AsNoTracking()
            .AnyAsync(animal => animal.Id == animalId, cancellationToken);

        if (!animalExists)
        {
            return null;
        }

        var timelineEvents = await _dbContext.AnimalTimelineEvents
            .AsNoTracking()
            .Include(timelineEvent => timelineEvent.Animal)
            .Include(timelineEvent => timelineEvent.Enclosure)
            .Where(timelineEvent => timelineEvent.AnimalId == animalId)
            .OrderByDescending(timelineEvent => timelineEvent.OccurredAt)
            .ThenByDescending(timelineEvent => timelineEvent.Id)
            .ToListAsync(cancellationToken);

        return timelineEvents.Select(ToDto).ToList();
    }

    public async Task<AnimalTimelineEventDto?> GetByIdAsync(
        int animalId,
        long eventId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.AnimalTimelineEvents
            .AsNoTracking()
            .Include(timelineEvent => timelineEvent.Animal)
            .Include(timelineEvent => timelineEvent.Enclosure)
            .Where(timelineEvent => timelineEvent.Id == eventId && timelineEvent.AnimalId == animalId)
            .SingleOrDefaultAsync(cancellationToken);

        return timelineEvent is null
            ? null
            : ToDto(timelineEvent);
    }

    public async Task<CreateAnimalTimelineEventResult> CreateAsync(
        int animalId,
        CreateAnimalTimelineEventDto request,
        CancellationToken cancellationToken = default)
    {
        var animalName = await _dbContext.Animals
            .Where(animal => animal.Id == animalId)
            .Select(animal => animal.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (animalName is null)
        {
            return CreateAnimalTimelineEventResult.AnimalNotFound();
        }

        if (request.EnclosureId is not { } enclosureId)
        {
            return CreateAnimalTimelineEventResult.EnclosureNotFound();
        }

        var enclosureName = await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (enclosureName is null)
        {
            return CreateAnimalTimelineEventResult.EnclosureNotFound();
        }

        var now = DateTime.UtcNow;
        var timelineEvent = new AnimalTimelineEvent
        {
            AnimalId = animalId,
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

        _dbContext.AnimalTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAnimalTimelineEventResult.Created(ToDto(timelineEvent, animalName, enclosureName));
    }

    public async Task<UpdateAnimalTimelineEventResult> UpdateAsync(
        int animalId,
        long eventId,
        UpdateAnimalTimelineEventDto request,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.AnimalTimelineEvents
            .SingleOrDefaultAsync(
                timelineEvent => timelineEvent.Id == eventId && timelineEvent.AnimalId == animalId,
                cancellationToken);

        if (timelineEvent is null)
        {
            return UpdateAnimalTimelineEventResult.NotFound();
        }

        if (request.EnclosureId is not { } enclosureId)
        {
            return UpdateAnimalTimelineEventResult.EnclosureNotFound();
        }

        var enclosureName = await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (enclosureName is null)
        {
            return UpdateAnimalTimelineEventResult.EnclosureNotFound();
        }

        timelineEvent.EnclosureId = enclosureId;
        timelineEvent.EventType = request.EventType!.Value;
        timelineEvent.OccurredAt = request.OccurredAt!.Value;
        timelineEvent.Title = request.Title!;
        timelineEvent.Description = request.Description;
        timelineEvent.PerformedBy = request.PerformedBy;
        timelineEvent.Metadata = ToJsonDocument(request.Metadata);
        timelineEvent.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var animalName = await _dbContext.Animals
            .Where(animal => animal.Id == animalId)
            .Select(animal => animal.Name)
            .SingleAsync(cancellationToken);

        return UpdateAnimalTimelineEventResult.Updated(ToDto(timelineEvent, animalName, enclosureName));
    }

    public async Task<DeleteAnimalTimelineEventResult> DeleteAsync(
        int animalId,
        long eventId,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = await _dbContext.AnimalTimelineEvents
            .SingleOrDefaultAsync(
                timelineEvent => timelineEvent.Id == eventId && timelineEvent.AnimalId == animalId,
                cancellationToken);

        if (timelineEvent is null)
        {
            return DeleteAnimalTimelineEventResult.NotFound;
        }

        _dbContext.AnimalTimelineEvents.Remove(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteAnimalTimelineEventResult.Deleted;
    }

    private static JsonDocument? ToJsonDocument(JsonElement? metadata)
    {
        return metadata.HasValue
            ? JsonDocument.Parse(metadata.Value.GetRawText())
            : null;
    }

    private static AnimalTimelineEventDto ToDto(AnimalTimelineEvent timelineEvent)
    {
        return ToDto(timelineEvent, timelineEvent.Animal.Name, timelineEvent.Enclosure.Name);
    }

    private static AnimalTimelineEventDto ToDto(
        AnimalTimelineEvent timelineEvent,
        string animalName,
        string enclosureName)
    {
        return new AnimalTimelineEventDto
        {
            Id = timelineEvent.Id,
            AnimalId = timelineEvent.AnimalId,
            AnimalName = animalName,
            EnclosureId = timelineEvent.EnclosureId,
            EnclosureName = enclosureName,
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
