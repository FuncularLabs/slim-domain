using Microsoft.Extensions.DependencyInjection;
using Switchback.Data;

namespace Switchback.Application.Services;

public static class ServiceRegistration
{
    /// <summary>Everything a host needs. Hosts call this and never reference a data-access library themselves.</summary>
    public static IServiceCollection AddSwitchbackServices(this IServiceCollection services, string connectionString)
    {
        services.AddSwitchbackData(connectionString);   // EF Core: owns the schema, and the "before" road
        services.AddFunkyOrm(connectionString);         // FunkyORM: the converted slice

        services.AddScoped<EfIncludeChainRecentOrdersService>();
        services.AddScoped<EfProjectionRecentOrdersService>();
        services.AddScoped<RecentOrdersService>();
        return services;
    }
}
