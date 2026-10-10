using SupplierFeed.Api.Data.UnitOfWork;
using SupplierFeed.Api.Infrastructure;

namespace SupplierFeed.Api.Data;

/// <summary>
/// Dependency injection registration for the data layer.
/// </summary>
public static class DataAccessServiceCollectionExtensions
{
    /// <summary>
    /// Registers the clock, database options, connection factory, schema initializer and unit of work factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration containing the <c>Database</c> section.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();
        services.AddSingleton<IUnitOfWorkFactory, SqliteUnitOfWorkFactory>();

        return services;
    }
}
