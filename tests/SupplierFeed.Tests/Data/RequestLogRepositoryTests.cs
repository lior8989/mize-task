using SupplierFeed.Tests.TestSupport;

namespace SupplierFeed.Tests.Data;

public class RequestLogRepositoryTests
{
    [Fact]
    public async Task CountAfter_NoRows_ReturnsZero()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);

        Assert.Equal(0, await uow.RequestLog.CountAfterAsync("s1", 0, CancellationToken.None));
    }

    [Fact]
    public async Task CountAfter_LowerBoundIsExclusive()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 1_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 2_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 3_000, CancellationToken.None);

        var count = await uow.RequestLog.CountAfterAsync("s1", 1_000, CancellationToken.None);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAfter_OnlyCountsTheGivenSupplier()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 1_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s2", 1_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s2", 1_500, CancellationToken.None);

        Assert.Equal(1, await uow.RequestLog.CountAfterAsync("s1", 0, CancellationToken.None));
        Assert.Equal(2, await uow.RequestLog.CountAfterAsync("s2", 0, CancellationToken.None));
    }

    [Fact]
    public async Task GetEarliestAfter_ReturnsSmallestTimestampInRange()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 3_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 1_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 2_000, CancellationToken.None);

        var earliest = await uow.RequestLog.GetEarliestAfterAsync("s1", 1_000, CancellationToken.None);

        Assert.Equal(2_000, earliest);
    }

    [Fact]
    public async Task GetEarliestAfter_NoRowsInRange_ReturnsNull()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 1_000, CancellationToken.None);

        Assert.Null(await uow.RequestLog.GetEarliestAfterAsync("s1", 1_000, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAtOrBefore_RemovesOnlyOldRowsOfThatSupplier()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 1_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 2_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s1", 3_000, CancellationToken.None);
        await uow.RequestLog.AddAsync("s2", 1_000, CancellationToken.None);

        var deleted = await uow.RequestLog.DeleteAtOrBeforeAsync("s1", 2_000, CancellationToken.None);

        Assert.Equal(2, deleted);
        Assert.Equal(1, await uow.RequestLog.CountAfterAsync("s1", 0, CancellationToken.None));
        Assert.Equal(1, await uow.RequestLog.CountAfterAsync("s2", 0, CancellationToken.None));
    }
}
