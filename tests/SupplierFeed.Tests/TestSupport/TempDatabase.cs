using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SupplierFeed.Api.Data;
using SupplierFeed.Api.Data.UnitOfWork;

namespace SupplierFeed.Tests.TestSupport;

/// <summary>
/// A throwaway SQLite database file with the real schema applied, deleted on dispose.
/// </summary>
public sealed class TempDatabase : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"supplierfeed-test-{Guid.NewGuid():N}.db");

    private TempDatabase()
    {
        // Pooling is off so the file is released as soon as a connection is disposed.
        ConnectionString = $"Data Source={_path};Pooling=False;Default Timeout=5";
        ConnectionFactory = new SqliteConnectionFactory(Options.Create(new DatabaseOptions { ConnectionString = ConnectionString }));
        UnitOfWorkFactory = new SqliteUnitOfWorkFactory(ConnectionFactory);
    }

    public string ConnectionString { get; }

    public IDbConnectionFactory ConnectionFactory { get; }

    public IUnitOfWorkFactory UnitOfWorkFactory { get; }

    public static async Task<TempDatabase> CreateAsync()
    {
        var database = new TempDatabase();
        await new SqliteDatabaseInitializer(database.ConnectionFactory).InitializeAsync(CancellationToken.None);
        return database;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }
}
