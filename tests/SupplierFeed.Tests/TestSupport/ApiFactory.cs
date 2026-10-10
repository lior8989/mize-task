using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SupplierFeed.Tests.TestSupport;

/// <summary>
/// Hosts the API against its own throwaway database instead of the default <c>supplierfeed.db</c>.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly Task<TempDatabase> _database = TempDatabase.CreateAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = _database.GetAwaiter().GetResult().ConnectionString;
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Database:ConnectionString"] = connectionString }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _database.GetAwaiter().GetResult().Dispose();
        }
    }
}
