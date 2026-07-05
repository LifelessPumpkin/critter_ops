using Microsoft.EntityFrameworkCore;
using Npgsql;
using skipper_api.Data;
using skipper_api.Domain.Enclosures;
using skipper_api.Dtos.Enclosures;

namespace skipper_api.Services.Enclosures;

public class EnclosureService : IEnclosureService
{
    private readonly ProfessorDbContext _dbContext;

    public EnclosureService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EnclosureResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Enclosures
            .AsNoTracking()
            .OrderBy(enclosure => enclosure.Name)
            .Select(enclosure => ToResponseDto(enclosure))
            .ToListAsync(cancellationToken);
    }

    public async Task<EnclosureResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Enclosures
            .AsNoTracking()
            .Where(enclosure => enclosure.Id == id)
            .Select(enclosure => ToResponseDto(enclosure))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<EnclosureResponseDto> CreateAsync(CreateEnclosureRequestDto request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var enclosure = new Enclosure
        {
            Name = request.Name!,
            Type = request.Type!.Value,
            Location = request.Location!,
            SizeLabel = request.SizeLabel,
            Length = request.Length,
            Width = request.Width,
            Height = request.Height,
            DimensionUnit = request.DimensionUnit,
            Volume = request.Volume,
            VolumeUnit = request.VolumeUnit,
            Material = request.Material,
            MaxAnimalCapacity = request.MaxAnimalCapacity,
            Mobility = request.Mobility!.Value,
            Status = request.Status!.Value,
            SafetyRating = request.SafetyRating,
            Notes = request.Notes,
            CreatedDate = now,
            UpdatedDate = now,
        };

        _dbContext.Enclosures.Add(enclosure);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponseDto(enclosure);
    }

    public async Task<EnclosureResponseDto?> UpdateAsync(
        int id,
        UpdateEnclosureRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var enclosure = await _dbContext.Enclosures
            .SingleOrDefaultAsync(enclosure => enclosure.Id == id, cancellationToken);

        if (enclosure is null)
        {
            return null;
        }

        enclosure.Name = request.Name!;
        enclosure.Type = request.Type!.Value;
        enclosure.Location = request.Location!;
        enclosure.SizeLabel = request.SizeLabel;
        enclosure.Length = request.Length;
        enclosure.Width = request.Width;
        enclosure.Height = request.Height;
        enclosure.DimensionUnit = request.DimensionUnit;
        enclosure.Volume = request.Volume;
        enclosure.VolumeUnit = request.VolumeUnit;
        enclosure.Material = request.Material;
        enclosure.MaxAnimalCapacity = request.MaxAnimalCapacity;
        enclosure.Mobility = request.Mobility!.Value;
        enclosure.Status = request.Status!.Value;
        enclosure.SafetyRating = request.SafetyRating;
        enclosure.Notes = request.Notes;
        enclosure.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponseDto(enclosure);
    }

    public async Task<DeleteEnclosureResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var enclosure = await _dbContext.Enclosures
            .SingleOrDefaultAsync(enclosure => enclosure.Id == id, cancellationToken);

        if (enclosure is null)
        {
            return DeleteEnclosureResult.NotFound;
        }

        _dbContext.Enclosures.Remove(enclosure);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
        {
            return DeleteEnclosureResult.HasAssignedAnimals;
        }

        return DeleteEnclosureResult.Deleted;
    }

    private static bool IsForeignKeyViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.ForeignKeyViolation;
    }

    private static EnclosureResponseDto ToResponseDto(Enclosure enclosure)
    {
        return new EnclosureResponseDto
        {
            Id = enclosure.Id,
            Name = enclosure.Name,
            Type = enclosure.Type,
            Location = enclosure.Location,
            SizeLabel = enclosure.SizeLabel,
            Length = enclosure.Length,
            Width = enclosure.Width,
            Height = enclosure.Height,
            DimensionUnit = enclosure.DimensionUnit,
            Volume = enclosure.Volume,
            VolumeUnit = enclosure.VolumeUnit,
            Material = enclosure.Material,
            MaxAnimalCapacity = enclosure.MaxAnimalCapacity,
            Mobility = enclosure.Mobility,
            Status = enclosure.Status,
            SafetyRating = enclosure.SafetyRating,
            Notes = enclosure.Notes,
            CreatedDate = enclosure.CreatedDate,
            UpdatedDate = enclosure.UpdatedDate,
        };
    }
}
