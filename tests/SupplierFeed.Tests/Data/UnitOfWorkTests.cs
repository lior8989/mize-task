using SupplierFeed.Tests.TestSupport;

namespace SupplierFeed.Tests.Data;

public class UnitOfWorkTests
{
    [Fact]
    public async Task DisposeWithoutCommit_RollsBackAllRepositories()
    {
        using var db = await TempDatabase.CreateAsync();
        await using (var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None))
        {
            await write.Stats.IncrementIngestedAsync("s1", CancellationToken.None);
            await write.RequestLog.AddAsync("s1", 1_000, CancellationToken.None);
        }

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        Assert.Null(await read.Stats.GetAsync("s1", CancellationToken.None));
        Assert.Equal(0, await read.RequestLog.CountAfterAsync("s1", 0, CancellationToken.None));
    }

    [Fact]
    public async Task Commit_PersistsChangesAcrossRepositoriesAtomically()
    {
        using var db = await TempDatabase.CreateAsync();
        await using (var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None))
        {
            await write.Stats.IncrementIngestedAsync("s1", CancellationToken.None);
            await write.RequestLog.AddAsync("s1", 1_000, CancellationToken.None);
            await write.CommitAsync(CancellationToken.None);
        }

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        Assert.Equal(1, (await read.Stats.GetAsync("s1", CancellationToken.None))?.IngestedCount);
        Assert.Equal(1, await read.RequestLog.CountAfterAsync("s1", 0, CancellationToken.None));
    }

    [Fact]
    public async Task ReadUnitOfWork_DoesNotBlockWhileAWriterIsActive()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);
        await write.Stats.IncrementIngestedAsync("s1", CancellationToken.None);

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        var stats = await read.Stats.GetAsync("s1", CancellationToken.None);

        Assert.Null(stats); // uncommitted write is invisible, and the read did not wait or fail
    }

    [Fact]
    public async Task ConcurrentWriters_AreSerializedAndNoIncrementIsLost()
    {
        using var db = await TempDatabase.CreateAsync();
        const int writers = 20;

        await Task.WhenAll(Enumerable.Range(0, writers).Select(async _ =>
        {
            await using var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);
            await uow.Stats.IncrementIngestedAsync("s1", CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);
        }));

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        Assert.Equal(writers, (await read.Stats.GetAsync("s1", CancellationToken.None))?.IngestedCount);
    }
}
