using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Switchback.Shared.Contracts;

namespace Switchback.Demo;

/// <summary>Runs the three variants for the same page and prints what each one cost.</summary>
public static class Scoreboard
{
    private const int WarmUps = 3;
    private const int TimedRuns = 20;
    private const int FieldsUsed = 5;   // RecentOrderResponse has five members

    public static async Task RunAsync(string connectionString, int page, string? country)
    {
        var results = new List<(Variant Variant, VariantRun Run, double MedianMs)>();

        foreach (var variant in Variants.All)
        {
            VariantRun? first = null;
            var timings = new List<double>(TimedRuns);

            for (var i = 0; i < WarmUps + TimedRuns; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var run = await variant.RunAsync(connectionString, page, country);
                stopwatch.Stop();

                first ??= run;
                if (i >= WarmUps) timings.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            results.Add((variant, first!, Median(timings)));
        }

        Print(results, page, country);
    }

    private static void Print(List<(Variant Variant, VariantRun Run, double MedianMs)> results, int page, string? country)
    {
        var rows = results[0].Run.Rows.Count;
        var title = country is null
            ? $"Recent orders, page {page} ({rows} rows)"
            : $"Recent orders to {country}, page {page} ({rows} rows)";

        Console.WriteLine($"{title,-46}  fields loaded   fields used   objects tracked   median ms*");
        Console.WriteLine(new string('-', 110));

        for (var i = 0; i < results.Count; i++)
        {
            var (variant, run, medianMs) = results[i];
            Console.WriteLine(
                $"{i + 1}  {variant.Name,-43}  {SqlColumnCounter.Count(run.Sql),13}   {FieldsUsed,11}   {run.TrackedObjects,15}   {medianMs,10:F1}");
        }

        Console.WriteLine();

        var reference = results[0].Run.Rows;
        var identical = results.All(r => r.Run.Rows.SequenceEqual(reference));
        if (identical)
        {
            Console.WriteLine($"Rows identical across variants: YES (sha256 {Fingerprint(reference)})");
        }
        else
        {
            Console.WriteLine("Rows identical across variants: NO");
            foreach (var (variant, run, _) in results)
                Console.WriteLine($"  {variant.Name}: sha256 {Fingerprint(run.Rows)}");
        }

        Console.WriteLine($"* this machine, warm cache, median of {TimedRuns} runs after {WarmUps} warm-ups. Illustrative only.");
    }

    private static double Median(List<double> values)
    {
        var sorted = values.Order().ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) / 2 : sorted[middle];
    }

    /// <summary>A short, stable hash of the rows: the same seed gives the same hash on every machine.</summary>
    public static string Fingerprint(IReadOnlyList<RecentOrderResponse> rows)
    {
        var json = JsonSerializer.Serialize(rows);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        return $"{hash[..4]}…{hash[^4..]}";
    }
}
