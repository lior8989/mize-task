namespace SupplierFeed.Api.Domain;

/// <summary>
/// Cumulative request counters for one supplier.
/// </summary>
/// <param name="SupplierId">The supplier the counters belong to.</param>
/// <param name="IngestedCount">Reservations created or updated.</param>
/// <param name="UnchangedCount">Duplicate requests that changed nothing.</param>
/// <param name="ThrottledCount">Requests rejected by the rate limit.</param>
public sealed record SupplierStats(
    string SupplierId,
    long IngestedCount,
    long UnchangedCount,
    long ThrottledCount);
