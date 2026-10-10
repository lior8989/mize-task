using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Services;

/// <summary>
/// Applies throttling and deduplication to incoming supplier reservations.
/// </summary>
public interface IReservationIngestService
{
    /// <summary>
    /// Ingests one reservation update: enforces the supplier's rate limit, then creates, updates
    /// or ignores the reservation, and records the matching statistics, all in one transaction.
    /// </summary>
    /// <param name="incoming">The reservation as sent by the supplier.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>What happened to the request.</returns>
    Task<IngestResult> IngestAsync(Reservation incoming, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a supplier's cumulative counters. A supplier that has never been seen has all zeros.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The counters.</returns>
    Task<SupplierStats> GetStatsAsync(string supplierId, CancellationToken cancellationToken);
}
