using System.Globalization;

namespace Switchback.Demo;

/// <summary>Command-line options: <c>--page N</c>, <c>--country NAME</c>, and one positional value.</summary>
public sealed record DemoOptions(int Page, string? Country, string? Positional)
{
    /// <exception cref="ArgumentException">An option is missing its value, or the value is not what it should be.</exception>
    public static DemoOptions Parse(IEnumerable<string> args)
    {
        var page = 0;
        string? country = null;
        string? positional = null;

        var queue = new Queue<string>(args);
        while (queue.Count > 0)
        {
            var arg = queue.Dequeue();
            switch (arg.ToLowerInvariant())
            {
                case "--page":
                    var value = Require(queue, arg);
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out page) || page < 0)
                        throw new ArgumentException($"--page needs a whole number of 0 or more, not '{value}'.");
                    break;
                case "--country":
                    country = Require(queue, arg);
                    break;
                default:
                    positional ??= arg;
                    break;
            }
        }

        return new DemoOptions(page, country, positional);
    }

    private static string Require(Queue<string> queue, string option) =>
        queue.Count > 0 ? queue.Dequeue() : throw new ArgumentException($"{option} needs a value.");
}
