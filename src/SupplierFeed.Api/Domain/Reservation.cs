namespace SupplierFeed.Api.Domain;

/// <summary>
/// A reservation as stored by the service. Identified by the supplier that sent it and the
/// supplier's own reservation id.
/// </summary>
/// <param name="SupplierId">The supplier that owns the reservation.</param>
/// <param name="ReservationId">The reservation id, unique within the supplier.</param>
/// <param name="RoomId">The reserved room.</param>
/// <param name="CheckIn">Check-in instant (UTC).</param>
/// <param name="CheckOut">Check-out instant (UTC).</param>
/// <param name="Price">The reservation price, stored exactly as a decimal.</param>
/// <param name="UpdatedAtUtc">When the supplier last changed the reservation, as reported by the supplier.</param>
/// <param name="CreatedAtUtc">When this service first stored the reservation.</param>
/// <param name="LastModifiedAtUtc">When this service last changed the stored reservation.</param>
public sealed record Reservation(
    string SupplierId,
    string ReservationId,
    string RoomId,
    DateTimeOffset CheckIn,
    DateTimeOffset CheckOut,
    decimal Price,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastModifiedAtUtc);
