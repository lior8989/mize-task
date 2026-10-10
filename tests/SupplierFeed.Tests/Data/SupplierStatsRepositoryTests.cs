using SupplierFeed.Api.Domain;
using SupplierFeed.Tests.TestSupport;

namespace SupplierFeed.Tests.Data;

public class SupplierStatsRepositoryTests
{
    [Fact]
    public async Task Get_UnknownSupplier_ReturnsNull()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);

        Assert.Null(await uow.Stats.GetAsync("nobody", CancellationToken.None));
    }

    [Fact]
    public async Task Increments_CreateRowAndCountEachCounterSeparately()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);

        await uow.Stats.IncrementIngestedAsync("s1", CancellationToken.None);
        await uow.Stats.IncrementIngestedAsync("s1", CancellationToken.None);
        await uow.Stats.IncrementUnchangedAsync("s1", CancellationToken.None);
        await uow.Stats.IncrementThrottledAsync("s1", CancellationToken.None);
        await uow.Stats.IncrementThrottledAsync("s1", CancellationToken.None);
        await uow.Stats.IncrementThrottledAsync("s1", CancellationToken.None);

        var stats = await uow.Stats.GetAsync("s1", CancellationToken.None);
        Assert.Equal(new SupplierStats("s1", 2, 1, 3), stats);
    }

    [Fact]
    public async Task Increments_AreIsolatedPerSupplier()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);

        await uow.Stats.IncrementIngestedAsync("s1", CancellationToken.None);
        await uow.Stats.IncrementThrottledAsync("s2", CancellationToken.None);

        Assert.Equal(new SupplierStats("s1", 1, 0, 0), await uow.Stats.GetAsync("s1", CancellationToken.None));
        Assert.Equal(new SupplierStats("s2", 0, 0, 1), await uow.Stats.GetAsync("s2", CancellationToken.None));
    }
}
