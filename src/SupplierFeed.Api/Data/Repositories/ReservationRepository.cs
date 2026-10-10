using System.Data.Common;
using System.Globalization;
using Dapper;
using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Data.Repositories;

/// <summary>
/// Dapper/SQLite implementation of <see cref="IReservationRepository"/>.
/// </summary>
public sealed class ReservationRepository : IReservationRepository
{
    private const string TimestampFormat = "O";

    private const string SelectSql = """
        SELECT SupplierId, ReservationId, RoomId, CheckIn, CheckOut, Price,
               UpdatedAtUtc, CreatedAtUtc, LastModifiedAtUtc
        FROM Reservations
        WHERE SupplierId = @SupplierId AND ReservationId = @ReservationId
        """;

    private const string InsertSql = """
        INSERT INTO Reservations
            (SupplierId, ReservationId, RoomId, CheckIn, CheckOut, Price,
             UpdatedAtUtc, CreatedAtUtc, LastModifiedAtUtc)
        VALUES
            (@SupplierId, @ReservationId, @RoomId, @CheckIn, @CheckOut, @Price,
             @UpdatedAtUtc, @CreatedAtUtc, @LastModifiedAtUtc)
        """;

    private const string UpdateSql = """
        UPDATE Reservations
        SET RoomId = @RoomId, CheckIn = @CheckIn, CheckOut = @CheckOut, Price = @Price,
            UpdatedAtUtc = @UpdatedAtUtc, LastModifiedAtUtc = @LastModifiedAtUtc
        WHERE SupplierId = @SupplierId AND ReservationId = @ReservationId
        """;

    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;

    /// <summary>
    /// Initializes the repository on an existing connection and transaction.
    /// </summary>
    /// <param name="connection">The open connection.</param>
    /// <param name="transaction">The transaction all commands run in.</param>
    public ReservationRepository(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    /// <inheritdoc />
    public async Task<Reservation?> GetAsync(string supplierId, string reservationId, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            SelectSql,
            new { SupplierId = supplierId, ReservationId = reservationId },
            _transaction,
            cancellationToken: cancellationToken);

        var row = await _connection.QuerySingleOrDefaultAsync<ReservationRow>(command);
        return row?.ToReservation();
    }

    /// <inheritdoc />
    public Task InsertAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            InsertSql, ReservationRow.From(reservation), _transaction, cancellationToken: cancellationToken);
        return _connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            UpdateSql, ReservationRow.From(reservation), _transaction, cancellationToken: cancellationToken);
        return await _connection.ExecuteAsync(command) > 0;
    }

    /// <summary>
    /// Storage shape of a reservation: decimals and timestamps are persisted as invariant TEXT so
    /// they round-trip exactly, and no global Dapper type handlers are needed.
    /// </summary>
    private sealed class ReservationRow
    {
        public string SupplierId { get; set; } = "";
        public string ReservationId { get; set; } = "";
        public string RoomId { get; set; } = "";
        public string CheckIn { get; set; } = "";
        public string CheckOut { get; set; } = "";
        public string Price { get; set; } = "";
        public string UpdatedAtUtc { get; set; } = "";
        public string CreatedAtUtc { get; set; } = "";
        public string LastModifiedAtUtc { get; set; } = "";

        public static ReservationRow From(Reservation r) => new()
        {
            SupplierId = r.SupplierId,
            ReservationId = r.ReservationId,
            RoomId = r.RoomId,
            CheckIn = FormatTimestamp(r.CheckIn),
            CheckOut = FormatTimestamp(r.CheckOut),
            Price = r.Price.ToString(CultureInfo.InvariantCulture),
            UpdatedAtUtc = FormatTimestamp(r.UpdatedAtUtc),
            CreatedAtUtc = FormatTimestamp(r.CreatedAtUtc),
            LastModifiedAtUtc = FormatTimestamp(r.LastModifiedAtUtc),
        };

        public Reservation ToReservation() => new(
            SupplierId,
            ReservationId,
            RoomId,
            ParseTimestamp(CheckIn),
            ParseTimestamp(CheckOut),
            decimal.Parse(Price, NumberStyles.Number, CultureInfo.InvariantCulture),
            ParseTimestamp(UpdatedAtUtc),
            ParseTimestamp(CreatedAtUtc),
            ParseTimestamp(LastModifiedAtUtc));

        private static string FormatTimestamp(DateTimeOffset value) =>
            value.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);

        private static DateTimeOffset ParseTimestamp(string value) =>
            DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }
}
