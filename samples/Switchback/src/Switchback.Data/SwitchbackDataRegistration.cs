using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Switchback.Data;

public static class SwitchbackDataRegistration
{
    /// <summary>Registers the EF Core context (scoped, the EF default). Lives here so hosts never reference EF directly.</summary>
    public static IServiceCollection AddSwitchbackData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SwitchbackDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }
}
