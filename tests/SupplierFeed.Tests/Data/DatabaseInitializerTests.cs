using Dapper;
using SupplierFeed.Api.Data;
using SupplierFeed.Tests.TestSupport;

namespace SupplierFeed.Tests.Data;

public class DatabaseInitializerTests
{
    [Fact]
    public async Task Initialize_CreatesAllTables()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var connection = await db.ConnectionFactory.OpenConnectionAsync(CancellationToken.None);

        var tables = (await connection.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type = 'table'")).ToList();

        Assert.Contains("Reservations", tables);
        Assert.Contains("RequestLog", tables);
        Assert.Contains("SupplierStats", tables);
    }

    [Fact]
    public async Task Initialize_CreatesRequestLogIndex()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var connection = await db.ConnectionFactory.OpenConnectionAsync(CancellationToken.None);

        var indexes = (await connection.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type = 'index'")).ToList();

        Assert.Contains("IX_RequestLog_Supplier_Time", indexes);
    }

    [Fact]
    public async Task Initialize_EnablesWriteAheadLogging()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var connection = await db.ConnectionFactory.OpenConnectionAsync(CancellationToken.None);

        var mode = await connection.ExecuteScalarAsync<string>("PRAGMA journal_mode");

        Assert.Equal("wal", mode);
    }

    [Fact]
    public async Task Initialize_CalledTwice_DoesNotFailOrLoseData()
    {
        using var db = await TempDatabase.CreateAsync();
        await using (var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None))
        {
            await uow.Stats.IncrementIngestedAsync("s1", CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);
        }

        await new SqliteDatabaseInitializer(db.ConnectionFactory).InitializeAsync(CancellationToken.None);

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        var stats = await read.Stats.GetAsync("s1", CancellationToken.None);
        Assert.Equal(1, stats?.IngestedCount);
    }
}
