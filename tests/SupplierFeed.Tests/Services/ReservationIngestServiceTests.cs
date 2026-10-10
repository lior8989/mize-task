using Microsoft.Extensions.Options;
using SupplierFeed.Api.Domain;
using SupplierFeed.Api.Services;
using SupplierFeed.Tests.TestSupport;

namespace SupplierFeed.Tests.Services;

public class ReservationIngestServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

    private static Reservation Make(string supplier = "s1", string id = "r1", string room = "room-1", decimal price = 450m, DateTimeOffset? updated = null) =>
        new(supplier, id, room, new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), new(2026, 8, 3, 0, 0, 0, TimeSpan.Zero),
            price, updated ?? Start, Start, Start);

    private static ReservationIngestService Create(TempDatabase db, FakeClock clock, int max = 100) =>
        new(db.UnitOfWorkFactory, clock, Options.Create(new ThrottleOptions { MaxRequests = max, Window = TimeSpan.FromSeconds(60) }));

    private static async Task<Reservation?> Load(TempDatabase db, string supplier = "s1", string id = "r1")
    {
        await using var uow = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        return await uow.Reservations.GetAsync(supplier, id, CancellationToken.None);
    }

    [Fact]
    public async Task NewReservation_IsCreated()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start));

        var result = await service.IngestAsync(Make(), CancellationToken.None);

        Assert.Equal(IngestOutcome.Created, result.Outcome);
        Assert.Equal(450m, (await Load(db))!.Price);
    }

    [Fact]
    public async Task ExactDuplicate_IsUnchangedAndNotWrittenAgain()
    {
        using var db = await TempDatabase.CreateAsync();
        var clock = new FakeClock(Start);
        var service = Create(db, clock);
        await service.IngestAsync(Make(), CancellationToken.None);
        var first = await Load(db);

        clock.Advance(TimeSpan.FromSeconds(5));
        var result = await service.IngestAsync(Make(), CancellationToken.None);

        Assert.Equal(IngestOutcome.Unchanged, result.Outcome);
        Assert.Equal(first!.LastModifiedAtUtc, (await Load(db))!.LastModifiedAtUtc);
        var stats = await service.GetStatsAsync("s1", CancellationToken.None);
        Assert.Equal(1, stats.IngestedCount);
        Assert.Equal(1, stats.UnchangedCount);
    }

    [Fact]
    public async Task ChangedDetails_UpdateTheExistingReservation()
    {
        using var db = await TempDatabase.CreateAsync();
        var clock = new FakeClock(Start);
        var service = Create(db, clock);
        await service.IngestAsync(Make(), CancellationToken.None);

        clock.Advance(TimeSpan.FromSeconds(5));
        var result = await service.IngestAsync(Make(price: 500m, updated: Start.AddMinutes(1)), CancellationToken.None);

        var stored = await Load(db);
        Assert.Equal(IngestOutcome.Updated, result.Outcome);
        Assert.Equal(500m, stored!.Price);
        Assert.Equal(Start, stored.CreatedAtUtc);
        Assert.Equal(2, (await service.GetStatsAsync("s1", CancellationToken.None)).IngestedCount);
    }

    [Fact]
    public async Task OlderUpdate_WithDifferentDetails_IsStaleAndKeepsStoredData()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start));
        await service.IngestAsync(Make(price: 500m, updated: Start.AddMinutes(5)), CancellationToken.None);

        var result = await service.IngestAsync(Make(price: 450m, updated: Start), CancellationToken.None);

        Assert.Equal(IngestOutcome.Stale, result.Outcome);
        Assert.Equal(500m, (await Load(db))!.Price);
    }

    [Fact]
    public async Task OnlyUpdatedAtDiffers_IsTreatedAsUnchanged()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start));
        await service.IngestAsync(Make(), CancellationToken.None);

        var result = await service.IngestAsync(Make(updated: Start.AddHours(1)), CancellationToken.None);

        Assert.Equal(IngestOutcome.Unchanged, result.Outcome);
    }

    [Fact]
    public async Task SameReservationId_ForDifferentSuppliers_AreIndependent()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start));
        await service.IngestAsync(Make("a"), CancellationToken.None);

        var result = await service.IngestAsync(Make("b"), CancellationToken.None);

        Assert.Equal(IngestOutcome.Created, result.Outcome);
    }

    [Fact]
    public async Task RequestOverTheLimit_IsThrottledAndCounted()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start), max: 3);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(IngestOutcome.Created, (await service.IngestAsync(Make(id: $"r{i}"), CancellationToken.None)).Outcome);
        }

        var result = await service.IngestAsync(Make(id: "r3"), CancellationToken.None);

        Assert.Equal(IngestOutcome.Throttled, result.Outcome);
        Assert.Null(await Load(db, id: "r3"));
        var stats = await service.GetStatsAsync("s1", CancellationToken.None);
        Assert.Equal(3, stats.IngestedCount);
        Assert.Equal(1, stats.ThrottledCount);
    }

    [Fact]
    public async Task ExactlyAtTheLimit_IsAllowed_AndTheNextOneIsNot()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start));
        for (var i = 0; i < 100; i++)
        {
            Assert.NotEqual(IngestOutcome.Throttled, (await service.IngestAsync(Make(id: $"r{i}"), CancellationToken.None)).Outcome);
        }

        Assert.Equal(IngestOutcome.Throttled, (await service.IngestAsync(Make(id: "extra"), CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task DuplicatesCountTowardTheLimit()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start), max: 2);
        await service.IngestAsync(Make(), CancellationToken.None);
        await service.IngestAsync(Make(), CancellationToken.None);

        Assert.Equal(IngestOutcome.Throttled, (await service.IngestAsync(Make(), CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task Throttling_IsPerSupplier()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start), max: 1);
        await service.IngestAsync(Make("a"), CancellationToken.None);

        Assert.Equal(IngestOutcome.Throttled, (await service.IngestAsync(Make("a", "r2"), CancellationToken.None)).Outcome);
        Assert.Equal(IngestOutcome.Created, (await service.IngestAsync(Make("b"), CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task WindowIsRolling_OldRequestsAgeOutOneByOne()
    {
        using var db = await TempDatabase.CreateAsync();
        var clock = new FakeClock(Start);
        var service = Create(db, clock, max: 2);
        await service.IngestAsync(Make(id: "r1"), CancellationToken.None);   // t=0
        clock.Advance(TimeSpan.FromSeconds(30));
        await service.IngestAsync(Make(id: "r2"), CancellationToken.None);   // t=30

        clock.Advance(TimeSpan.FromSeconds(29));                             // t=59: first still in window
        var blocked = await service.IngestAsync(Make(id: "r3"), CancellationToken.None);
        Assert.Equal(IngestOutcome.Throttled, blocked.Outcome);
        Assert.Equal(TimeSpan.FromSeconds(1), blocked.RetryAfter);

        clock.Advance(TimeSpan.FromSeconds(1));                              // t=60: first aged out, second still counts
        Assert.Equal(IngestOutcome.Created, (await service.IngestAsync(Make(id: "r3"), CancellationToken.None)).Outcome);
        Assert.Equal(IngestOutcome.Throttled, (await service.IngestAsync(Make(id: "r4"), CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task ThrottledRequests_DoNotExtendThePenalty()
    {
        using var db = await TempDatabase.CreateAsync();
        var clock = new FakeClock(Start);
        var service = Create(db, clock, max: 1);
        await service.IngestAsync(Make(id: "r1"), CancellationToken.None);
        for (var i = 0; i < 5; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(10));
            await service.IngestAsync(Make(id: "spam"), CancellationToken.None);
        }

        clock.Advance(TimeSpan.FromSeconds(10)); // 60s after the only accepted request
        Assert.Equal(IngestOutcome.Created, (await service.IngestAsync(Make(id: "r2"), CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task ConcurrentRequests_NeverExceedTheLimit()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start), max: 10);

        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(i =>
            Task.Run(() => service.IngestAsync(Make(id: $"r{i}"), CancellationToken.None))));

        Assert.Equal(10, results.Count(r => r.Outcome == IngestOutcome.Created));
        Assert.Equal(30, results.Count(r => r.Outcome == IngestOutcome.Throttled));
        var stats = await service.GetStatsAsync("s1", CancellationToken.None);
        Assert.Equal(10, stats.IngestedCount);
        Assert.Equal(30, stats.ThrottledCount);
    }

    [Fact]
    public async Task ConcurrentIdenticalRequests_CreateOnceAndTheRestAreUnchanged()
    {
        using var db = await TempDatabase.CreateAsync();
        var service = Create(db, new FakeClock(Start));

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            Task.Run(() => service.IngestAsync(Make(), CancellationToken.None))));

        Assert.Equal(1, results.Count(r => r.Outcome == IngestOutcome.Created));
        Assert.Equal(19, results.Count(r => r.Outcome == IngestOutcome.Unchanged));
    }

    [Fact]
    public async Task UnknownSupplier_HasZeroStats()
    {
        using var db = await TempDatabase.CreateAsync();
        var stats = await Create(db, new FakeClock(Start)).GetStatsAsync("nobody", CancellationToken.None);

        Assert.Equal(new SupplierStats("nobody", 0, 0, 0), stats);
    }
}
