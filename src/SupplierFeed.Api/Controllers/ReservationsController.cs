using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SupplierFeed.Api.Contracts;
using SupplierFeed.Api.Domain;
using SupplierFeed.Api.Services;

namespace SupplierFeed.Api.Controllers;

/// <summary>
/// Reservation ingestion and per-supplier statistics.
/// </summary>
[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReservationIngestService _service;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    /// <param name="service">The ingest service.</param>
    public ReservationsController(IReservationIngestService service)
    {
        _service = service;
    }

    /// <summary>
    /// Ingests a reservation update. New reservations are created (201), changed ones are updated (200),
    /// and exact duplicates are acknowledged without writing (200, <c>changed: false</c>).
    /// </summary>
    /// <param name="request">The reservation.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <response code="201">A new reservation was stored.</response>
    /// <response code="200">An existing reservation was updated, or the request changed nothing.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="429">The supplier exceeded 100 requests in 60 seconds; see the Retry-After header.</response>
    [HttpPost("ingest")]
    [ProducesResponseType<IngestResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<IngestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Ingest([FromBody] IngestReservationRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var reservation = new Reservation(
            request.SupplierId,
            request.ReservationId,
            request.RoomId,
            request.CheckIn.ToUniversalTime(),
            request.CheckOut.ToUniversalTime(),
            request.Price,
            request.UpdatedAtUtc.ToUniversalTime(),
            now,
            now);

        var result = await _service.IngestAsync(reservation, cancellationToken);
        switch (result.Outcome)
        {
            case IngestOutcome.Throttled:
                var seconds = (int)Math.Ceiling(Math.Max(result.RetryAfter?.TotalSeconds ?? 1, 1));
                Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                return Problem(
                    title: "Too many requests",
                    detail: $"Supplier '{request.SupplierId}' exceeded its rate limit. Retry in {seconds} seconds.",
                    statusCode: StatusCodes.Status429TooManyRequests);
            case IngestOutcome.Created:
                return StatusCode(StatusCodes.Status201Created, new IngestResponse(result.Outcome, true));
            default:
                return Ok(new IngestResponse(result.Outcome, result.Outcome == IngestOutcome.Updated));
        }
    }

    /// <summary>
    /// Gets how many reservations were ingested and how many requests were throttled for a supplier.
    /// A supplier never seen before returns zeros.
    /// </summary>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <response code="200">The supplier's counters.</response>
    [HttpGet("stats/{supplierId}")]
    [ProducesResponseType<SupplierStatsResponse>(StatusCodes.Status200OK)]
    public async Task<SupplierStatsResponse> GetStats(string supplierId, CancellationToken cancellationToken)
    {
        var stats = await _service.GetStatsAsync(supplierId, cancellationToken);
        return new SupplierStatsResponse(stats.SupplierId, stats.IngestedCount, stats.UnchangedCount, stats.ThrottledCount);
    }
}
