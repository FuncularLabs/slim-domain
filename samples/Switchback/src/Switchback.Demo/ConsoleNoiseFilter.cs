using System.Text;

namespace Switchback.Demo;

/// <summary>
/// FunkyORM 3.10.0 prints "Generated ORDER BY clause: …" to stdout for every ordered query, a leftover
/// debug line (see FUNKYORM_FEEDBACK.md). This keeps that one line off the screen and passes everything
/// else through untouched.
/// </summary>
public sealed class ConsoleNoiseFilter : TextWriter
{
    private const string NoisePrefix = "Generated ORDER BY clause";

    private readonly TextWriter _inner;

    private ConsoleNoiseFilter(TextWriter inner) => _inner = inner;

    public override Encoding Encoding => _inner.Encoding;

    /// <summary>Installs the filter on <see cref="Console.Out"/>; dispose the result to restore the original writer.</summary>
    public static IDisposable Install()
    {
        var original = Console.Out;
        Console.SetOut(new ConsoleNoiseFilter(original));
        return new Restore(original);
    }

    public override void Write(char value) => _inner.Write(value);

    public override void Write(string? value) => _inner.Write(value);

    public override void WriteLine(string? value)
    {
        if (value is not null && value.StartsWith(NoisePrefix, StringComparison.Ordinal)) return;
        _inner.WriteLine(value);
    }

    public override void Flush() => _inner.Flush();

    private sealed class Restore(TextWriter original) : IDisposable
    {
        public void Dispose() => Console.SetOut(original);
    }
}
