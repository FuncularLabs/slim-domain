using System.Text.RegularExpressions;

namespace Switchback.Demo;

/// <summary>
/// Makes a statement readable on screen without changing its tokens. FunkyORM puts FROM and every JOIN on
/// one line and splits OFFSET from FETCH; this gives each JOIN its own line and rejoins the page clause.
/// EF Core formats its own SQL, and that text passes through unchanged.
/// </summary>
public static partial class SqlFormatter
{
    public static string ForScreen(string sql)
    {
        var output = new List<string>();

        foreach (var line in sql.Trim().ReplaceLineEndings("\n").Split('\n'))
        {
            if (output.Count > 0
                && line.TrimStart().StartsWith("FETCH", StringComparison.OrdinalIgnoreCase)
                && output[^1].TrimStart().StartsWith("OFFSET", StringComparison.OrdinalIgnoreCase))
            {
                output[^1] += " " + line.Trim();
                continue;
            }

            output.Add(JoinInsideLine().Replace(line, m => Environment.NewLine + "  " + m.Groups[1].Value));
        }

        return string.Join(Environment.NewLine, output);
    }

    // A JOIN that follows other text on the same line (never one that already starts a line).
    [GeneratedRegex(@"(?<=\S)\s+((?:LEFT|RIGHT|INNER|FULL)(?: OUTER)? JOIN)\b", RegexOptions.IgnoreCase)]
    private static partial Regex JoinInsideLine();
}
