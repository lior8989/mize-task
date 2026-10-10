using System.Data.Common;

namespace SupplierFeed.Api.Data;

/// <summary>
/// Creates open database connections.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Opens a new connection. The caller owns it and must dispose it.
    /// </summary>
    /// <param name="cancellationToken">Cancels the open operation.</param>
    /// <returns>An open connection.</returns>
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
