namespace SupplierFeed.Api.Data;

/// <summary>
/// Prepares the database schema.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Creates the tables and indexes if they do not exist. Safe to call repeatedly.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task InitializeAsync(CancellationToken cancellationToken);
}
