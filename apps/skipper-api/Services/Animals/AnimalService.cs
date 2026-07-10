using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using skipper_api.Dtos.Animals;

namespace skipper_api.Services.Animals;

public class AnimalService : IAnimalService
{
    private readonly ProfessorDbContext _dbContext;

    public AnimalService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Animals
            .AsNoTracking()
            .OrderBy(animal => animal.Name)
            .Select(ToResponseDtoProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<AnimalResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Animals
            .AsNoTracking()
            .Where(animal => animal.Id == id)
            .Select(ToResponseDtoProjection)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CreateAnimalResult> CreateAsync(
        CreateAnimalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.EnclosureId is not { } enclosureId)
        {
            return CreateAnimalResult.EnclosureNotFound();
        }

        var enclosureName = await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (enclosureName is null)
        {
            return CreateAnimalResult.EnclosureNotFound();
        }

        var now = DateTime.UtcNow;
        var animal = new Animal
        {
            EnclosureId = enclosureId,
            Enclosure = null!,
            Name = request.Name!,
            Species = request.Species!,
            SubspeciesOrMorph = request.SubspeciesOrMorph,
            AnimalType = request.AnimalType!.Value,
            Status = request.Status!.Value,
            Sex = request.Sex!.Value,
            BirthDate = request.BirthDate,
            BirthDateIsEstimated = request.BirthDateIsEstimated,
            AcquiredDate = request.AcquiredDate!.Value,
            DispositionDate = request.DispositionDate,
            DispositionReason = request.DispositionReason,
            MicrochipNumber = request.MicrochipNumber,
            TagIdentifier = request.TagIdentifier,
            Source = request.Source,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAnimalResult.Created(ToResponseDto(animal, enclosureName));
    }

    public async Task<UpdateAnimalResult> UpdateAsync(
        int id,
        UpdateAnimalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var animal = await _dbContext.Animals
            .SingleOrDefaultAsync(animal => animal.Id == id, cancellationToken);

        if (animal is null)
        {
            return UpdateAnimalResult.AnimalNotFound();
        }

        if (request.EnclosureId is not { } enclosureId)
        {
            return UpdateAnimalResult.EnclosureNotFound();
        }

        var enclosureName = await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (enclosureName is null)
        {
            return UpdateAnimalResult.EnclosureNotFound();
        }

        animal.EnclosureId = enclosureId;
        animal.Name = request.Name!;
        animal.Species = request.Species!;
        animal.SubspeciesOrMorph = request.SubspeciesOrMorph;
        animal.AnimalType = request.AnimalType!.Value;
        animal.Status = request.Status!.Value;
        animal.Sex = request.Sex!.Value;
        animal.BirthDate = request.BirthDate;
        animal.BirthDateIsEstimated = request.BirthDateIsEstimated;
        animal.AcquiredDate = request.AcquiredDate!.Value;
        animal.DispositionDate = request.DispositionDate;
        animal.DispositionReason = request.DispositionReason;
        animal.MicrochipNumber = request.MicrochipNumber;
        animal.TagIdentifier = request.TagIdentifier;
        animal.Source = request.Source;
        animal.Notes = request.Notes;
        animal.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateAnimalResult.Updated(ToResponseDto(animal, enclosureName));
    }

    public async Task<DeleteAnimalResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var animal = await _dbContext.Animals
            .SingleOrDefaultAsync(animal => animal.Id == id, cancellationToken);

        if (animal is null)
        {
            return DeleteAnimalResult.NotFound;
        }

        _dbContext.Animals.Remove(animal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteAnimalResult.Deleted;
    }

    private static readonly Expression<Func<Animal, AnimalResponseDto>> ToResponseDtoProjection = animal =>
        new AnimalResponseDto
        {
            Id = animal.Id,
            EnclosureId = animal.EnclosureId,
            EnclosureName = animal.Enclosure.Name,
            Name = animal.Name,
            Species = animal.Species,
            SubspeciesOrMorph = animal.SubspeciesOrMorph,
            AnimalType = animal.AnimalType,
            Status = animal.Status,
            Sex = animal.Sex,
            BirthDate = animal.BirthDate,
            BirthDateIsEstimated = animal.BirthDateIsEstimated,
            AcquiredDate = animal.AcquiredDate,
            DispositionDate = animal.DispositionDate,
            DispositionReason = animal.DispositionReason,
            MicrochipNumber = animal.MicrochipNumber,
            TagIdentifier = animal.TagIdentifier,
            Source = animal.Source,
            Notes = animal.Notes,
            CreatedAt = animal.CreatedAt,
            UpdatedAt = animal.UpdatedAt,
        };

    private static AnimalResponseDto ToResponseDto(Animal animal, string enclosureName)
    {
        return new AnimalResponseDto
        {
            Id = animal.Id,
            EnclosureId = animal.EnclosureId,
            EnclosureName = enclosureName,
            Name = animal.Name,
            Species = animal.Species,
            SubspeciesOrMorph = animal.SubspeciesOrMorph,
            AnimalType = animal.AnimalType,
            Status = animal.Status,
            Sex = animal.Sex,
            BirthDate = animal.BirthDate,
            BirthDateIsEstimated = animal.BirthDateIsEstimated,
            AcquiredDate = animal.AcquiredDate,
            DispositionDate = animal.DispositionDate,
            DispositionReason = animal.DispositionReason,
            MicrochipNumber = animal.MicrochipNumber,
            TagIdentifier = animal.TagIdentifier,
            Source = animal.Source,
            Notes = animal.Notes,
            CreatedAt = animal.CreatedAt,
            UpdatedAt = animal.UpdatedAt,
        };
    }
}
