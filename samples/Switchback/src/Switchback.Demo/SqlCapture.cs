using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Switchback.Demo;

/// <summary>Records the SQL EF Core sends, so the scoreboard measures the statement that actually ran.</summary>
public sealed class EfSqlCapture : DbCommandInterceptor
{
    private readonly List<string> _statements = [];

    public IReadOnlyList<string> Statements => _statements;

    public string LastSelect =>
        _statements.LastOrDefault(IsSelect) ?? throw new InvalidOperationException("No SELECT was captured.");

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        _statements.Add(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        _statements.Add(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private static bool IsSelect(string statement) =>
        statement.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Records what FunkyORM logs: the statement, then one line per parameter.</summary>
public sealed class FunkyOrmSqlCapture
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public string LastSelect =>
        _messages.LastOrDefault(m => m.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("No SELECT was captured.");

    public void Log(string message) => _messages.Add(message);
}
