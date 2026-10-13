using Switchback.Data;
using Switchback.Demo;

var connectionString = SwitchbackConnection.Resolve();
var command = args.Length == 0 ? "scoreboard" : args[0].ToLowerInvariant();

Console.OutputEncoding = System.Text.Encoding.UTF8;
using var quiet = ConsoleNoiseFilter.Install();   // FunkyORM 3.10.0 debug line; see FUNKYORM_FEEDBACK.md
Console.WriteLine($"Switchback — {SwitchbackConnection.Describe(connectionString)}");
Console.WriteLine();

DemoOptions options;
try
{
    options = DemoOptions.Parse(args.Skip(1));
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    Usage();
    return 1;
}

try
{
    switch (command)
    {
        case "seed":
            await SwitchbackDatabase.EnsureReadyAsync(connectionString, Console.WriteLine);
            break;

        case "reset":
            await SwitchbackDatabase.ResetAsync(connectionString, Console.WriteLine);
            break;

        case "scoreboard":
            await SwitchbackDatabase.EnsureReadyAsync(connectionString, Console.WriteLine);
            Console.WriteLine();
            await Scoreboard.RunAsync(connectionString, options.Page, options.Country);
            break;

        case "sql":
            var variant = Variants.Find(options.Positional);
            if (variant is null)
            {
                Usage();
                return 1;
            }
            await SwitchbackDatabase.EnsureReadyAsync(connectionString, Console.WriteLine);
            var run = await variant.RunAsync(connectionString, options.Page, options.Country);
            Console.WriteLine();
            Console.WriteLine($"{variant.Name}:");
            Console.WriteLine();
            Console.WriteLine(SqlFormatter.ForScreen(run.Sql));
            Console.WriteLine();
            Console.WriteLine($"{SqlColumnCounter.Count(run.Sql)} columns in the outer SELECT list; {run.Rows.Count} rows; {run.TrackedObjects} objects tracked.");
            break;

        default:
            Usage();
            return 1;
    }
}
catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
{
    // The database is not the seed (SwitchbackDatabase.EnsureReadyAsync), or the page is impossible
    // (Paging.Validate): say so, without a stack trace.
    Console.WriteLine(ex.Message);
    return 1;
}

return 0;

static void Usage()
{
    Console.WriteLine("""
        Usage:
          Switchback.Demo                                        seed if needed, then the scoreboard
          Switchback.Demo scoreboard [--page N] [--country NAME]
          Switchback.Demo sql include|projection|funky [--page N] [--country NAME]
          Switchback.Demo seed                                   create the schema and the data if missing
          Switchback.Demo reset                                  clear the four tables and seed them again
        """);
}
