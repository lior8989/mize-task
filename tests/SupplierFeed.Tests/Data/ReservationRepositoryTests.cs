using Microsoft.Data.Sqlite;
using SupplierFeed.Api.Domain;
using SupplierFeed.Tests.TestSupport;

namespace SupplierFeed.Tests.Data;

public class ReservationRepositoryTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 1, 10, 15, 0, TimeSpan.Zero);

    private static Reservation Sample(string supplierId = "s1", string reservationId = "r1", decimal price = 450.00m) => new(
        supplierId,
        reservationId,
        "room-1",
        new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero),
        price,
        UpdatedAtUtc: T0,
        CreatedAtUtc: T0.AddMinutes(1),
        LastModifiedAtUtc: T0.AddMinutes(1));

    [Fact]
    public async Task Get_UnknownReservation_ReturnsNull()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var uow = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);

        var result = await uow.Reservations.GetAsync("s1", "missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task InsertThenGet_RoundTripsAllFieldsExactly()
    {
        using var db = await TempDatabase.CreateAsync();
        var reservation = Sample(price: 450.10m);
        await using (var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None))
        {
            await write.Reservations.InsertAsync(reservation, CancellationToken.None);
            await write.CommitAsync(CancellationToken.None);
        }

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        var loaded = await read.Reservations.GetAsync("s1", "r1", CancellationToken.None);

        Assert.Equal(reservation, loaded);
    }

    [Fact]
    public async Task Insert_NonUtcOffset_IsStoredAsTheSameInstant()
    {
        using var db = await TempDatabase.CreateAsync();
        var local = Sample() with { CheckIn = new DateTimeOffset(2026, 8, 1, 2, 0, 0, TimeSpan.FromHours(2)) };
        await using (var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None))
        {
            await write.Reservations.InsertAsync(local, CancellationToken.None);
            await write.CommitAsync(CancellationToken.None);
        }

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        var loaded = await read.Reservations.GetAsync("s1", "r1", CancellationToken.None);

        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), loaded!.CheckIn);
    }

    [Fact]
    public async Task Insert_DuplicateKey_Throws()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);
        await write.Reservations.InsertAsync(Sample(), CancellationToken.None);

        await Assert.ThrowsAsync<SqliteException>(() =>
            write.Reservations.InsertAsync(Sample(), CancellationToken.None));
    }

    [Fact]
    public async Task Insert_SameReservationIdForDifferentSuppliers_StoresBoth()
    {
        using var db = await TempDatabase.CreateAsync();
        await using (var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None))
        {
            await write.Reservations.InsertAsync(Sample("s1", "r1"), CancellationToken.None);
            await write.Reservations.InsertAsync(Sample("s2", "r1"), CancellationToken.None);
            await write.CommitAsync(CancellationToken.None);
        }

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        Assert.NotNull(await read.Reservations.GetAsync("s1", "r1", CancellationToken.None));
        Assert.NotNull(await read.Reservations.GetAsync("s2", "r1", CancellationToken.None));
    }

    [Fact]
    public async Task Update_ExistingReservation_ChangesDetailsAndKeepsCreatedAt()
    {
        using var db = await TempDatabase.CreateAsync();
        var original = Sample();
        var changed = original with
        {
            RoomId = "room-2",
            Price = 500m,
            UpdatedAtUtc = T0.AddHours(1),
            LastModifiedAtUtc = T0.AddHours(2),
            CreatedAtUtc = T0.AddYears(5), // must be ignored by the update
        };
        await using (var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None))
        {
            await write.Reservations.InsertAsync(original, CancellationToken.None);
            var updated = await write.Reservations.UpdateAsync(changed, CancellationToken.None);
            await write.CommitAsync(CancellationToken.None);
            Assert.True(updated);
        }

        await using var read = await db.UnitOfWorkFactory.BeginReadAsync(CancellationToken.None);
        var loaded = await read.Reservations.GetAsync("s1", "r1", CancellationToken.None);
        Assert.Equal(changed with { CreatedAtUtc = original.CreatedAtUtc }, loaded);
    }

    [Fact]
    public async Task Update_UnknownReservation_ReturnsFalse()
    {
        using var db = await TempDatabase.CreateAsync();
        await using var write = await db.UnitOfWorkFactory.BeginWriteAsync(CancellationToken.None);

        var updated = await write.Reservations.UpdateAsync(Sample(), CancellationToken.None);

        Assert.False(updated);
    }
}
