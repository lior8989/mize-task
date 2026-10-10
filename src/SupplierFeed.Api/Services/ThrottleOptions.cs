using System.ComponentModel.DataAnnotations;

namespace SupplierFeed.Api.Services;

/// <summary>
/// Per-supplier rate limit settings, bound from the <c>Throttle</c> configuration section.
/// </summary>
public sealed class ThrottleOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Throttle";

    /// <summary>
    /// Gets or sets the maximum number of requests a supplier may make within <see cref="Window"/>.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// Gets or sets the length of the rolling window.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(60);
}
