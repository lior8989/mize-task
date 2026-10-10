namespace SupplierFeed.Api.Services;

/// <summary>
/// What an ingest request did.
/// </summary>
public enum IngestOutcome
{
    /// <summary>A new reservation was stored.</summary>
    Created,

    /// <summary>An existing reservation was overwritten with new details.</summary>
    Updated,

    /// <summary>The reservation was already stored with the same details; nothing was written.</summary>
    Unchanged,

    /// <summary>
    /// The request carried different details but an older <c>updatedAtUtc</c> than the stored reservation
    /// (an out-of-order retry), so the stored reservation was kept.
    /// </summary>
    Stale,

    /// <summary>The supplier exceeded its rate limit and the request was rejected.</summary>
    Throttled
}

/// <summary>
/// The result of an ingest request.
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="RetryAfter">For <see cref="IngestOutcome.Throttled"/>, how long until the supplier may retry.</param>
public sealed record IngestResult(IngestOutcome Outcome, TimeSpan? RetryAfter = null);
