using SupplierFeed.Api.Services;

namespace SupplierFeed.Api.Contracts;

/// <summary>
/// The result of an ingest request.
/// </summary>
/// <param name="Outcome">What happened: Created, Updated, Unchanged or Stale.</param>
/// <param name="Changed">Whether the stored data was written.</param>
public sealed record IngestResponse(IngestOutcome Outcome, bool Changed);

/// <summary>
/// A supplier's cumulative request counters.
/// </summary>
/// <param name="SupplierId">The supplier.</param>
/// <param name="Ingested">Reservations created or updated.</param>
/// <param name="Unchanged">Duplicate requests that changed nothing.</param>
/// <param name="Throttled">Requests rejected by the rate limit.</param>
public sealed record SupplierStatsResponse(string SupplierId, long Ingested, long Unchanged, long Throttled);
