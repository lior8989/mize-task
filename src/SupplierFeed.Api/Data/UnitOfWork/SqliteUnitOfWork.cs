using System.Data.Common;
using SupplierFeed.Api.Data.Repositories;

namespace SupplierFeed.Api.Data.UnitOfWork;

/// <summary>
/// <see cref="IUnitOfWork"/> over a single SQLite connection and transaction.
/// </summary>
public sealed class SqliteUnitOfWork : IUnitOfWork
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;

    /// <summary>
    /// Initializes the unit of work. It takes ownership of both arguments and disposes them.
    /// </summary>
    /// <param name="connection">An open connection.</param>
    /// <param name="transaction">The transaction begun on <paramref name="connection"/>.</param>
    public SqliteUnitOfWork(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
        Reservations = new ReservationRepository(connection, transaction);
        RequestLog = new RequestLogRepository(connection, transaction);
        Stats = new SupplierStatsRepository(connection, transaction);
    }

    /// <inheritdoc />
    public IReservationRepository Reservations { get; }

    /// <inheritdoc />
    public IRequestLogRepository RequestLog { get; }

    /// <inheritdoc />
    public ISupplierStatsRepository Stats { get; }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Disposing an uncommitted transaction rolls it back.
        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
