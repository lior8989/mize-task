using System.Data.Common;
using Dapper;

namespace SupplierFeed.Api.Data.Repositories;

/// <summary>
/// Dapper/SQLite implementation of <see cref="IRequestLogRepository"/>. All queries are served by the
/// <c>IX_RequestLog_Supplier_Time</c> index.
/// </summary>
public sealed class RequestLogRepository : IRequestLogRepository
{
    private const string CountSql = """
        SELECT COUNT(*) FROM RequestLog
        WHERE SupplierId = @SupplierId AND RequestedAtMs > @AfterMs
        """;

    private const string EarliestSql = """
        SELECT MIN(RequestedAtMs) FROM RequestLog
        WHERE SupplierId = @SupplierId AND RequestedAtMs > @AfterMs
        """;

    private const string InsertSql = """
        INSERT INTO RequestLog (SupplierId, RequestedAtMs) VALUES (@SupplierId, @RequestedAtMs)
        """;

    private const string DeleteSql = """
        DELETE FROM RequestLog WHERE SupplierId = @SupplierId AND RequestedAtMs <= @AtOrBeforeMs
        """;

    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;

    /// <summary>
    /// Initializes the repository on an existing connection and transaction.
    /// </summary>
    /// <param name="connection">The open connection.</param>
    /// <param name="transaction">The transaction all commands run in.</param>
    public RequestLogRepository(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    /// <inheritdoc />
    public Task<int> CountAfterAsync(string supplierId, long afterUnixMs, CancellationToken cancellationToken) =>
        _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            CountSql,
            new { SupplierId = supplierId, AfterMs = afterUnixMs },
            _transaction,
            cancellationToken: cancellationToken));

    /// <inheritdoc />
    public Task<long?> GetEarliestAfterAsync(string supplierId, long afterUnixMs, CancellationToken cancellationToken) =>
        _connection.ExecuteScalarAsync<long?>(new CommandDefinition(
            EarliestSql,
            new { SupplierId = supplierId, AfterMs = afterUnixMs },
            _transaction,
            cancellationToken: cancellationToken));

    /// <inheritdoc />
    public Task AddAsync(string supplierId, long requestedAtUnixMs, CancellationToken cancellationToken) =>
        _connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            new { SupplierId = supplierId, RequestedAtMs = requestedAtUnixMs },
            _transaction,
            cancellationToken: cancellationToken));

    /// <inheritdoc />
    public Task<int> DeleteAtOrBeforeAsync(string supplierId, long atOrBeforeUnixMs, CancellationToken cancellationToken) =>
        _connection.ExecuteAsync(new CommandDefinition(
            DeleteSql,
            new { SupplierId = supplierId, AtOrBeforeMs = atOrBeforeUnixMs },
            _transaction,
            cancellationToken: cancellationToken));
}
