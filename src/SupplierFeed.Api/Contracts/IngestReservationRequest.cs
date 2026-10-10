using System.ComponentModel.DataAnnotations;

namespace SupplierFeed.Api.Contracts;

/// <summary>
/// A reservation update sent by a supplier.
/// </summary>
public sealed class IngestReservationRequest : IValidatableObject
{
    /// <summary>The supplier sending the update.</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200)]
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>The supplier's reservation id, unique within the supplier.</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200)]
    public string ReservationId { get; set; } = string.Empty;

    /// <summary>The reserved room.</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200)]
    public string RoomId { get; set; } = string.Empty;

    /// <summary>Check-in instant.</summary>
    public DateTimeOffset CheckIn { get; set; }

    /// <summary>Check-out instant; must be after <see cref="CheckIn"/>.</summary>
    public DateTimeOffset CheckOut { get; set; }

    /// <summary>The price; must not be negative.</summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    /// <summary>When the supplier last changed the reservation.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckIn == default)
        {
            yield return new ValidationResult("checkIn is required.", [nameof(CheckIn)]);
        }

        if (UpdatedAtUtc == default)
        {
            yield return new ValidationResult("updatedAtUtc is required.", [nameof(UpdatedAtUtc)]);
        }

        if (CheckOut <= CheckIn)
        {
            yield return new ValidationResult("checkOut must be after checkIn.", [nameof(CheckOut)]);
        }
    }
}
