namespace Switchback.Demo;

/// <summary>
/// Counts the columns in the outermost SELECT list of a statement: the items between the first SELECT and
/// its FROM, split on commas at parenthesis depth zero. Quoted strings and bracketed identifiers are skipped
/// whole, so a comma or a keyword inside them does not count. That number is "fields loaded" on the scoreboard.
/// </summary>
public static class SqlColumnCounter
{
    public static int Count(string sql)
    {
        var select = IndexOfWord(sql, "SELECT", 0);
        if (select < 0) throw new ArgumentException("Not a SELECT statement.", nameof(sql));

        var i = SkipModifiers(sql, select + "SELECT".Length);
        var depth = 0;
        var columns = 1;

        for (; i < sql.Length; i++)
        {
            var c = sql[i];
            if (c == '\'' || c == '"') i = EndOfQuoted(sql, i, c);
            else if (c == '[') i = EndOfBracketed(sql, i);
            else if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (depth == 0 && c == ',') columns++;
            else if (depth == 0 && IsWordAt(sql, "FROM", i)) return columns;
        }

        throw new ArgumentException("No FROM clause found.", nameof(sql));
    }

    // DISTINCT and TOP(n) / TOP n sit between SELECT and the first column.
    private static int SkipModifiers(string sql, int i)
    {
        while (true)
        {
            i = SkipWhitespace(sql, i);

            if (IsWordAt(sql, "DISTINCT", i))
            {
                i += "DISTINCT".Length;
                continue;
            }

            if (IsWordAt(sql, "TOP", i))
            {
                i = SkipWhitespace(sql, i + "TOP".Length);
                if (i < sql.Length && sql[i] == '(')
                {
                    i = sql.IndexOf(')', i) + 1;
                }
                else
                {
                    while (i < sql.Length && char.IsDigit(sql[i])) i++;
                }
                continue;
            }

            return i;
        }
    }

    // Index of the closing quote; a doubled quote inside the literal is an escaped quote, not the end.
    private static int EndOfQuoted(string sql, int start, char quote)
    {
        for (var i = start + 1; i < sql.Length; i++)
        {
            if (sql[i] != quote) continue;
            if (i + 1 < sql.Length && sql[i + 1] == quote) { i++; continue; }
            return i;
        }

        throw new ArgumentException("Unterminated string literal.", nameof(sql));
    }

    private static int EndOfBracketed(string sql, int start)
    {
        var end = sql.IndexOf(']', start + 1);
        return end < 0 ? throw new ArgumentException("Unterminated bracketed identifier.", nameof(sql)) : end;
    }

    private static int SkipWhitespace(string sql, int i)
    {
        while (i < sql.Length && char.IsWhiteSpace(sql[i])) i++;
        return i;
    }

    private static int IndexOfWord(string sql, string word, int from)
    {
        for (var i = sql.IndexOf(word, from, StringComparison.OrdinalIgnoreCase);
             i >= 0;
             i = sql.IndexOf(word, i + 1, StringComparison.OrdinalIgnoreCase))
        {
            if (IsWordAt(sql, word, i)) return i;
        }

        return -1;
    }

    private static bool IsWordAt(string sql, string word, int i) =>
        i >= 0
        && i + word.Length <= sql.Length
        && string.Compare(sql, i, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) == 0
        && (i == 0 || !IsWordChar(sql[i - 1]))
        && (i + word.Length == sql.Length || !IsWordChar(sql[i + word.Length]));

    // A dot counts as a word character so that an unbracketed Orders.From is never mistaken for FROM.
    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '.';
}
