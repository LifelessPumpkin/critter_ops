using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Npgsql;
using skipper_api.Data;

namespace HowellSync.Persistence;

/// <summary>One physical, nonpooled session owns both the advisory lock and every operation context.</summary>
public sealed class CatalogSession : IAsyncDisposable
{
    // Stable CritterOps catalog-wide key. All future catalog writers must use this key.
    public const long LockKey = 0x4352495454434154;
    private readonly NpgsqlConnection connection;
    private CatalogSession(NpgsqlConnection connection) => this.connection = connection;

    public static async Task<CatalogSession?> TryOpenAsync(string connectionString, CancellationToken token)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false, Multiplexing = false };
        var connection = new NpgsqlConnection(settings.ConnectionString);
        try
        {
            await connection.OpenAsync(token);
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", LockKey);
            if ((bool)(await command.ExecuteScalarAsync(token))!) return new(connection);
            await connection.DisposeAsync();
            return null;
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    public ProfessorDbContext CreateContext()
    {
        // Never reconnect after losing the owning session: reconnecting would lose mutual exclusion.
        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Catalog lock connection is no longer open.");
        return new(new DbContextOptionsBuilder<ProfessorDbContext>().UseNpgsql(connection).AddInterceptors(new NoReconnectInterceptor()).Options);
    }

    // EF can otherwise reopen a broken connection on a second query in the same context.
    // The session is already open before a context exists; any EF open attempt means lock loss.
    private sealed class NoReconnectInterceptor : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
            throw new InvalidOperationException("The catalog lock session cannot be reopened.");

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The catalog lock session cannot be reopened.");
    }

    // Closing a nonpooled physical connection releases all session locks even after rollback/failure.
    public ValueTask DisposeAsync() => connection.DisposeAsync();
}
