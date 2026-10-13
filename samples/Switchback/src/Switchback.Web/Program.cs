using Switchback.Application.Services;
using Switchback.Data;
using Switchback.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

var connectionString = SwitchbackConnection.Resolve(builder.Configuration.GetConnectionString("Switchback"));
builder.Services.AddSwitchbackServices(connectionString);

var app = builder.Build();

// First run: create the schema and seed 100,000 orders (15–30 seconds). Later runs: a no-op.
await SwitchbackDatabase.EnsureReadyAsync(connectionString, message => app.Logger.LogInformation("{Message}", message));

// The converted slice: a FunkyORM read model, mapped once at the edge to the published contract.
app.MapGet("/orders/recent", (RecentOrdersService orders, int page = 0, string? country = null) =>
    Paged(() => orders.GetPage(page, shipToCountry: country).Select(r => r.ToResponse()).ToList()));

// The "before" world and the EF fix, kept for comparison.
app.MapGet("/orders/recent/ef-include", (EfIncludeChainRecentOrdersService orders, int page = 0, string? country = null) =>
    PagedAsync(() => orders.GetPageAsync(page, shipToCountry: country)));

app.MapGet("/orders/recent/ef-projection", (EfProjectionRecentOrdersService orders, int page = 0, string? country = null) =>
    PagedAsync(() => orders.GetPageAsync(page, shipToCountry: country)));

app.Run();

// Paging.Validate decides what a bad page is; the endpoints only turn its refusal into a 400.
static IResult Paged(Func<IReadOnlyList<RecentOrderResponse>> run)
{
    try
    {
        return Results.Ok(run());
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}

static async Task<IResult> PagedAsync(Func<Task<IReadOnlyList<RecentOrderResponse>>> run)
{
    try
    {
        return Results.Ok(await run());
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}

// Lets the architecture tests see this assembly.
public partial class Program;
