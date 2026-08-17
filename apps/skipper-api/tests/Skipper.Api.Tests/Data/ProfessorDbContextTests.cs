using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Enclosures;
using Xunit;

namespace Skipper.Api.Tests.Data;

public class ProfessorDbContextTests
{
    [Fact]
    public async Task ProfessorDbContext_CanPersistAndRetrieveEnclosure()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ProfessorDbContext>()
            .UseSqlite(connection)
            .Options;

        int enclosureId;
        var createdAt = DateTime.UtcNow;

        await using (var setupContext = new ProfessorDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var enclosure = new Enclosure
            {
                Name = "Reptile Room Terrarium 1",
                Type = EnclosureType.Terrarium,
                Location = "Reptile Room",
                Mobility = EnclosureMobility.Movable,
                Status = EnclosureStatus.Active,
                CreatedDate = createdAt,
                UpdatedDate = createdAt,
            };

            setupContext.Enclosures.Add(enclosure);
            await setupContext.SaveChangesAsync();

            enclosureId = enclosure.Id;
        }

        await using var assertionContext = new ProfessorDbContext(options);
        var persistedEnclosure = await assertionContext.Enclosures
            .SingleAsync(enclosure => enclosure.Id == enclosureId);

        Assert.True(persistedEnclosure.Id > 0);
        Assert.Equal("Reptile Room Terrarium 1", persistedEnclosure.Name);
        Assert.Equal(EnclosureType.Terrarium, persistedEnclosure.Type);
        Assert.Equal("Reptile Room", persistedEnclosure.Location);
        Assert.Equal(EnclosureStatus.Active, persistedEnclosure.Status);
    }
}
