using System.Data.Common;
using Dapper;
using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Data.Repositories;

/// <summary>
/// Dapper/SQLite implementation of <see cref="ISupplierStatsRepository"/>. Counters use a single
/// atomic upsert statement each.
/// </summary>
public sealed class SupplierStatsRepository : ISupplierStatsRepository
{
    private const string SelectSql = """
        SELECT SupplierId, IngestedCount, UnchangedCount, ThrottledCount
        FROM SupplierStats WHERE SupplierId = @SupplierId
        """;

    private const string IncrementIngestedSql = """
        INSERT INTO SupplierStats (SupplierId, IngestedCount) VALUES (@SupplierId, 1)
        ON CONFLICT (SupplierId) DO UPDATE SET IngestedCount = IngestedCount + 1
        """;

    private const string IncrementUnchangedSql = """
        INSERT INTO SupplierStats (SupplierId, UnchangedCount) VALUES (@SupplierId, 1)
        ON CONFLICT (SupplierId) DO UPDATE SET UnchangedCount = UnchangedCount + 1
        """;

    private const string IncrementThrottledSql = """
        INSERT INTO SupplierStats (SupplierId, ThrottledCount) VALUES (@SupplierId, 1)
        ON CONFLICT (SupplierId) DO UPDATE SET ThrottledCount = ThrottledCount + 1
        """;

    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;

    /// <summary>
    /// Initializes the repository on an existing connection and transaction.
    /// </summary>
    /// <param name="connection">The open connection.</param>
    /// <param name="transaction">The transaction all commands run in.</param>
    public SupplierStatsRepository(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    /// <inheritdoc />
    public Task<SupplierStats?> GetAsync(string supplierId, CancellationToken cancellationToken) =>
        _connection.QuerySingleOrDefaultAsync<SupplierStats>(
            new CommandDefinition(SelectSql, new { SupplierId = supplierId }, _transaction, cancellationToken: cancellationToken));

    /// <inheritdoc />
    public Task IncrementIngestedAsync(string supplierId, CancellationToken cancellationToken) =>
        IncrementAsync(IncrementIngestedSql, supplierId, cancellationToken);

    /// <inheritdoc />
    public Task IncrementUnchangedAsync(string supplierId, CancellationToken cancellationToken) =>
        IncrementAsync(IncrementUnchangedSql, supplierId, cancellationToken);

    /// <inheritdoc />
    public Task IncrementThrottledAsync(string supplierId, CancellationToken cancellationToken) =>
        IncrementAsync(IncrementThrottledSql, supplierId, cancellationToken);

    private Task IncrementAsync(string sql, string supplierId, CancellationToken cancellationToken) =>
        _connection.ExecuteAsync(
            new CommandDefinition(sql, new { SupplierId = supplierId }, _transaction, cancellationToken: cancellationToken));
}
