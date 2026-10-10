using Microsoft.Extensions.Options;
using SupplierFeed.Api.Data.UnitOfWork;
using SupplierFeed.Api.Domain;
using SupplierFeed.Api.Infrastructure;

namespace SupplierFeed.Api.Services;

/// <inheritdoc />
public sealed class ReservationIngestService : IReservationIngestService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IClock _clock;
    private readonly ThrottleOptions _throttle;

    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <param name="unitOfWorkFactory">Starts transactions.</param>
    /// <param name="clock">The time source for the rolling window.</param>
    /// <param name="throttle">The rate limit settings.</param>
    public ReservationIngestService(IUnitOfWorkFactory unitOfWorkFactory, IClock clock, IOptions<ThrottleOptions> throttle)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _clock = clock;
        _throttle = throttle.Value;
    }

    /// <inheritdoc />
    public async Task<IngestResult> IngestAsync(Reservation incoming, CancellationToken cancellationToken)
    {
        // The write unit of work holds the database write lock from the start, so the count-then-insert
        // below is atomic across requests and across server instances sharing the database.
        await using var uow = await _unitOfWorkFactory.BeginWriteAsync(cancellationToken);

        var now = _clock.UtcNow;
        var nowMs = now.ToUnixTimeMilliseconds();
        var windowStartMs = now.Subtract(_throttle.Window).ToUnixTimeMilliseconds();
        var supplierId = incoming.SupplierId;

        if (await uow.RequestLog.CountAfterAsync(supplierId, windowStartMs, cancellationToken) >= _throttle.MaxRequests)
        {
            // The window clears when the oldest request still inside it ages out.
            var oldestMs = await uow.RequestLog.GetEarliestAfterAsync(supplierId, windowStartMs, cancellationToken) ?? nowMs;
            var retryAfter = TimeSpan.FromMilliseconds(oldestMs + _throttle.Window.TotalMilliseconds - nowMs);

            await uow.Stats.IncrementThrottledAsync(supplierId, cancellationToken);
            await uow.CommitAsync(cancellationToken);
            return new IngestResult(IngestOutcome.Throttled, retryAfter);
        }

        // Only accepted requests count toward the limit, so a throttled supplier's retries cannot extend its own penalty.
        await uow.RequestLog.AddAsync(supplierId, nowMs, cancellationToken);
        await uow.RequestLog.DeleteAtOrBeforeAsync(supplierId, windowStartMs, cancellationToken);

        var existing = await uow.Reservations.GetAsync(supplierId, incoming.ReservationId, cancellationToken);
        IngestOutcome outcome;
        if (existing is null)
        {
            await uow.Reservations.InsertAsync(incoming with { CreatedAtUtc = now, LastModifiedAtUtc = now }, cancellationToken);
            outcome = IngestOutcome.Created;
        }
        else if (SameDetails(existing, incoming))
        {
            outcome = IngestOutcome.Unchanged;
        }
        else if (incoming.UpdatedAtUtc < existing.UpdatedAtUtc)
        {
            outcome = IngestOutcome.Stale;
        }
        else
        {
            await uow.Reservations.UpdateAsync(incoming with { LastModifiedAtUtc = now }, cancellationToken);
            outcome = IngestOutcome.Updated;
        }

        if (outcome is IngestOutcome.Created or IngestOutcome.Updated)
        {
            await uow.Stats.IncrementIngestedAsync(supplierId, cancellationToken);
        }
        else
        {
            await uow.Stats.IncrementUnchangedAsync(supplierId, cancellationToken);
        }

        await uow.CommitAsync(cancellationToken);
        return new IngestResult(outcome);
    }

    /// <inheritdoc />
    public async Task<SupplierStats> GetStatsAsync(string supplierId, CancellationToken cancellationToken)
    {
        await using var uow = await _unitOfWorkFactory.BeginReadAsync(cancellationToken);
        return await uow.Stats.GetAsync(supplierId, cancellationToken) ?? new SupplierStats(supplierId, 0, 0, 0);
    }

    // updatedAtUtc is deliberately excluded: a resend that only bumps the supplier's timestamp is not a change.
    private static bool SameDetails(Reservation stored, Reservation incoming) =>
        stored.RoomId == incoming.RoomId
        && stored.CheckIn == incoming.CheckIn
        && stored.CheckOut == incoming.CheckOut
        && stored.Price == incoming.Price;
}
