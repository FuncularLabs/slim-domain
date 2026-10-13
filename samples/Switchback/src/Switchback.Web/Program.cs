using Switchback.Application.Services;
using Switchback.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = SwitchbackConnection.Resolve(builder.Configuration.GetConnectionString("Switchback"));
builder.Services.AddSwitchbackServices(connectionString);

var app = builder.Build();

// First run: create the schema and seed 100,000 orders (15–30 seconds). Later runs: a no-op.
await SwitchbackDatabase.EnsureReadyAsync(connectionString, message => app.Logger.LogInformation("{Message}", message));

// The converted slice: a FunkyORM read model, mapped once at the edge to the published contract.
app.MapGet("/orders/recent", (RecentOrdersService orders, int page = 0, string? country = null) =>
    page < 0
        ? Results.BadRequest("page must be 0 or more.")
        : Results.Ok(orders.GetPage(page, shipToCountry: country).Select(r => r.ToResponse())));

// The "before" world and the EF fix, kept for comparison.
app.MapGet("/orders/recent/ef-include", async (EfIncludeChainRecentOrdersService orders, int page = 0, string? country = null) =>
    page < 0
        ? Results.BadRequest("page must be 0 or more.")
        : Results.Ok(await orders.GetPageAsync(page, shipToCountry: country)));

app.MapGet("/orders/recent/ef-projection", async (EfProjectionRecentOrdersService orders, int page = 0, string? country = null) =>
    page < 0
        ? Results.BadRequest("page must be 0 or more.")
        : Results.Ok(await orders.GetPageAsync(page, shipToCountry: country)));

app.Run();

// Lets the architecture tests see this assembly.
public partial class Program;
