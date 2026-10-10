using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace SupplierFeed.Api.Data.UnitOfWork;

/// <summary>
/// <see cref="IUnitOfWorkFactory"/> for SQLite.
/// </summary>
public sealed class SqliteUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes the factory.
    /// </summary>
    /// <param name="connectionFactory">Opens the database connections.</param>
    public SqliteUnitOfWorkFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public Task<IUnitOfWork> BeginWriteAsync(CancellationToken cancellationToken) =>
        BeginAsync(deferred: false, cancellationToken);

    /// <inheritdoc />
    public Task<IUnitOfWork> BeginReadAsync(CancellationToken cancellationToken) =>
        BeginAsync(deferred: true, cancellationToken);

    private async Task<IUnitOfWork> BeginAsync(bool deferred, CancellationToken cancellationToken)
    {
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            // SQLite's BEGIN IMMEDIATE is only reachable through the provider-specific overload
            // (and the driver executes it synchronously); other providers get their default transaction.
            DbTransaction transaction = connection is SqliteConnection sqlite
                ? sqlite.BeginTransaction(deferred)
                : await connection.BeginTransactionAsync(cancellationToken);

            return new SqliteUnitOfWork(connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
