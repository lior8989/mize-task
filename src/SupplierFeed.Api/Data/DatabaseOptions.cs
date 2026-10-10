using System.ComponentModel.DataAnnotations;

namespace SupplierFeed.Api.Data;

/// <summary>
/// Database settings, bound from the <c>Database</c> configuration section.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Gets or sets the SQLite connection string. Include <c>Default Timeout</c> so concurrent
    /// writers wait for the lock instead of failing immediately.
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = "Data Source=supplierfeed.db;Default Timeout=5";
}
