using Microsoft.EntityFrameworkCore;
using Npgsql;
using skipper_api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Skipper.Api.Tests.Data;

// Each test gets its own database; only this disposable container supplies connections.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task<DbContextOptions<ProfessorDbContext>> CreateDatabaseAsync()
    {
        var name = "test_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE {name}", connection);
        await command.ExecuteNonQueryAsync();
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name };
        var options = new DbContextOptionsBuilder<ProfessorDbContext>().UseNpgsql(builder.ConnectionString).Options;
        await using var context = new ProfessorDbContext(options);
        await context.Database.MigrateAsync();
        return options;
    }
}
