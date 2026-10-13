using Microsoft.EntityFrameworkCore.Design;

namespace Switchback.Data;

/// <summary>Lets <c>dotnet ef</c> build the context when adding migrations.</summary>
public sealed class SwitchbackDbContextFactory : IDesignTimeDbContextFactory<SwitchbackDbContext>
{
    public SwitchbackDbContext CreateDbContext(string[] args) =>
        new(SwitchbackDbContext.CreateOptions(SwitchbackConnection.Resolve()));
}
