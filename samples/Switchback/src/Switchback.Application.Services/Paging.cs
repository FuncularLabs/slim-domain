namespace Switchback.Application.Services;

/// <summary>Paging rules shared by every Recent orders implementation, so the three agree on what a bad page is.</summary>
public static class Paging
{
    public const int DefaultPageSize = 50;

    /// <summary>
    /// Rejects a negative page, an empty page, and a page so large that <c>page * pageSize</c> overflows into
    /// a negative offset. Without the last check, EF fails in SQL while FunkyORM clamps to page 0.
    /// </summary>
    public static void Validate(int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, int.MaxValue / pageSize);
    }
}
