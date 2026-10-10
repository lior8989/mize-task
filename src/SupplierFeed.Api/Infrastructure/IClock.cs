namespace SupplierFeed.Api.Infrastructure;

/// <summary>
/// Source of the current time. Abstracted so time-dependent logic (such as the throttle window)
/// can be tested deterministically.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current instant in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
