using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Data.Repositories;

/// <summary>
/// Persistence operations for the <c>Reservations</c> table.
/// </summary>
public interface IReservationRepository
{
    /// <summary>
    /// Gets a reservation by its supplier and reservation id.
    /// </summary>
    /// <param name="supplierId">The owning supplier.</param>
    /// <param name="reservationId">The reservation id, unique within the supplier.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The reservation, or <see langword="null"/> if none exists.</returns>
    Task<Reservation?> GetAsync(string supplierId, string reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a new reservation.
    /// </summary>
    /// <param name="reservation">The reservation to store.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <exception cref="Microsoft.Data.Sqlite.SqliteException">A reservation with the same key already exists.</exception>
    Task InsertAsync(Reservation reservation, CancellationToken cancellationToken);

    /// <summary>
    /// Overwrites the mutable details of an existing reservation. <see cref="Reservation.CreatedAtUtc"/> is preserved.
    /// </summary>
    /// <param name="reservation">The new details, identified by supplier and reservation id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if a reservation was updated; <see langword="false"/> if none exists.</returns>
    Task<bool> UpdateAsync(Reservation reservation, CancellationToken cancellationToken);
}
