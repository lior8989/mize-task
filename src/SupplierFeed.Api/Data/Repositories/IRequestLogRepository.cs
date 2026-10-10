namespace SupplierFeed.Api.Data.Repositories;

/// <summary>
/// Persistence operations for the <c>RequestLog</c> table, the record of recent accepted requests
/// used to enforce the rolling throttle window. Timestamps are Unix milliseconds (UTC).
/// </summary>
public interface IRequestLogRepository
{
    /// <summary>
    /// Counts a supplier's logged requests strictly after the given instant.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="afterUnixMs">Exclusive lower bound, in Unix milliseconds.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The number of matching requests.</returns>
    Task<int> CountAfterAsync(string supplierId, long afterUnixMs, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the earliest logged request strictly after the given instant.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="afterUnixMs">Exclusive lower bound, in Unix milliseconds.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The earliest timestamp, or <see langword="null"/> if there are none.</returns>
    Task<long?> GetEarliestAfterAsync(string supplierId, long afterUnixMs, CancellationToken cancellationToken);

    /// <summary>
    /// Records an accepted request.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="requestedAtUnixMs">When the request arrived, in Unix milliseconds.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task AddAsync(string supplierId, long requestedAtUnixMs, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a supplier's requests at or before the given instant, keeping the table bounded.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="atOrBeforeUnixMs">Inclusive upper bound, in Unix milliseconds.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The number of rows deleted.</returns>
    Task<int> DeleteAtOrBeforeAsync(string supplierId, long atOrBeforeUnixMs, CancellationToken cancellationToken);
}
