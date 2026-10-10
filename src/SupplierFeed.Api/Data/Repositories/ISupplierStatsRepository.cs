using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Data.Repositories;

/// <summary>
/// Persistence operations for the <c>SupplierStats</c> counter table. Increments are atomic, so
/// concurrent requests never lose counts.
/// </summary>
public interface ISupplierStatsRepository
{
    /// <summary>
    /// Gets a supplier's counters.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The counters, or <see langword="null"/> if the supplier has never been seen.</returns>
    Task<SupplierStats?> GetAsync(string supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds one to the supplier's ingested (created or updated) count, creating the row if needed.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task IncrementIngestedAsync(string supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds one to the supplier's unchanged (duplicate) count, creating the row if needed.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task IncrementUnchangedAsync(string supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds one to the supplier's throttled count, creating the row if needed.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task IncrementThrottledAsync(string supplierId, CancellationToken cancellationToken);
}
