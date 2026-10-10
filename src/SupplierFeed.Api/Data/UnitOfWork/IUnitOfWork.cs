using SupplierFeed.Api.Data.Repositories;

namespace SupplierFeed.Api.Data.UnitOfWork;

/// <summary>
/// One database connection and transaction shared by all repositories. Changes are only persisted
/// by <see cref="CommitAsync"/>; disposing without committing rolls everything back.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Gets the reservations repository bound to this unit of work.
    /// </summary>
    IReservationRepository Reservations { get; }

    /// <summary>
    /// Gets the request log repository bound to this unit of work.
    /// </summary>
    IRequestLogRepository RequestLog { get; }

    /// <summary>
    /// Gets the supplier statistics repository bound to this unit of work.
    /// </summary>
    ISupplierStatsRepository Stats { get; }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task CommitAsync(CancellationToken cancellationToken);
}
