using Funcular.Data.Orm;
using Funcular.Data.Orm.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Switchback.Application.Services;

public static class FunkyOrmRegistration
{
    /// <summary>
    /// Registers the FunkyORM provider. Scoped, never singleton: one provider per request, so a transaction
    /// can never leak across requests (Slim Domain §2.3). The SQL it sends goes to the "FunkyORM" logger.
    /// </summary>
    public static IServiceCollection AddFunkyOrm(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<IOrmDataProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("FunkyORM");
            return CreateProvider(connectionString, message => logger.LogInformation("{Message}", message));
        });
        return services;
    }

    /// <summary>A provider outside DI (the console runner and the tests). The caller disposes it.</summary>
    public static IOrmDataProvider CreateProvider(string connectionString, Action<string>? log = null) =>
        new SqlServerOrmDataProvider(connectionString) { Log = log! };
}
