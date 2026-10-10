namespace SupplierFeed.Api.Data.UnitOfWork;

/// <summary>
/// Starts units of work. The caller owns the returned instance and must dispose it.
/// </summary>
public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Starts a unit of work that takes the database write lock immediately, so a read-then-write
    /// sequence (such as the throttle check followed by an insert) cannot interleave with another writer.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A new write unit of work.</returns>
    Task<IUnitOfWork> BeginWriteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts a unit of work for reading only. It does not take the write lock, so it never blocks writers.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A new read unit of work.</returns>
    Task<IUnitOfWork> BeginReadAsync(CancellationToken cancellationToken);
}
