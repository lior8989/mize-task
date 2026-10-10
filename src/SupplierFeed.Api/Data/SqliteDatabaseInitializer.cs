using Dapper;

namespace SupplierFeed.Api.Data;

/// <summary>
/// Creates the SQLite schema: reservations, the throttle request log, and supplier counters.
/// </summary>
public sealed class SqliteDatabaseInitializer : IDatabaseInitializer
{
    // WAL lets readers proceed while a writer is active and persists in the database file.
    // Prices are TEXT so decimals are stored exactly; timestamps are ISO 8601 UTC TEXT.
    private const string Schema = """
        PRAGMA journal_mode = WAL;

        CREATE TABLE IF NOT EXISTS Reservations (
            SupplierId        TEXT NOT NULL,
            ReservationId     TEXT NOT NULL,
            RoomId            TEXT NOT NULL,
            CheckIn           TEXT NOT NULL,
            CheckOut          TEXT NOT NULL,
            Price             TEXT NOT NULL,
            UpdatedAtUtc      TEXT NOT NULL,
            CreatedAtUtc      TEXT NOT NULL,
            LastModifiedAtUtc TEXT NOT NULL,
            PRIMARY KEY (SupplierId, ReservationId)
        ) WITHOUT ROWID;

        CREATE TABLE IF NOT EXISTS RequestLog (
            Id            INTEGER PRIMARY KEY,
            SupplierId    TEXT    NOT NULL,
            RequestedAtMs INTEGER NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_RequestLog_Supplier_Time
            ON RequestLog (SupplierId, RequestedAtMs);

        CREATE TABLE IF NOT EXISTS SupplierStats (
            SupplierId     TEXT    NOT NULL PRIMARY KEY,
            IngestedCount  INTEGER NOT NULL DEFAULT 0,
            UnchangedCount INTEGER NOT NULL DEFAULT 0,
            ThrottledCount INTEGER NOT NULL DEFAULT 0
        ) WITHOUT ROWID;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes the initializer.
    /// </summary>
    /// <param name="connectionFactory">Opens the database connection.</param>
    public SqliteDatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(Schema, cancellationToken: cancellationToken));
    }
}
