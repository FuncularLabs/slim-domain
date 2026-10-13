using Switchback.Demo;

namespace Switchback.Tests;

public class SqlColumnCounterTests
{
    [Fact]
    public void Counts_bracketed_columns_in_EF_style_SQL()
    {
        const string sql = """
            SELECT [o].[Id], [o].[OrderNumber], [c].[Name], [c0].[Name], [o].[Total]
            FROM [Orders] AS [o]
            INNER JOIN [Customers] AS [c] ON [o].[CustomerId] = [c].[Id]
            """;

        Assert.Equal(5, SqlColumnCounter.Count(sql));
    }

    [Fact]
    public void Counts_aliased_columns_in_FunkyORM_style_SQL()
    {
        const string sql = "SELECT Orders.Id, Orders.Total, [Customers_0].Name AS [CustomerName] FROM Orders LEFT JOIN Customers [Customers_0] ON Orders.CustomerId = [Customers_0].Id";

        Assert.Equal(3, SqlColumnCounter.Count(sql));
    }

    [Fact]
    public void Ignores_commas_inside_parentheses()
    {
        const string sql = "SELECT COALESCE([a].[X], [b].[Y]), CASE WHEN [a].[Z] IS NULL THEN 0 ELSE 1 END, CONVERT(nvarchar(10), [a].[D]) FROM [T] AS [a]";

        Assert.Equal(3, SqlColumnCounter.Count(sql));
    }

    [Fact]
    public void Skips_TOP_and_DISTINCT()
    {
        Assert.Equal(2, SqlColumnCounter.Count("SELECT TOP(50) [a], [b] FROM [T]"));
        Assert.Equal(2, SqlColumnCounter.Count("SELECT TOP 50 [a], [b] FROM [T]"));
        Assert.Equal(1, SqlColumnCounter.Count("SELECT DISTINCT [a] FROM [T]"));
    }

    [Fact]
    public void Counts_only_the_outer_list_when_a_subquery_sits_in_FROM()
    {
        const string sql = "SELECT [t].[a], [t].[b] FROM (SELECT [x], [y], [z] FROM [q]) AS [t]";

        Assert.Equal(2, SqlColumnCounter.Count(sql));
    }

    [Fact]
    public void A_column_named_like_FROM_is_not_the_FROM_clause()
    {
        Assert.Equal(2, SqlColumnCounter.Count("SELECT [o].[FromDate], [o].[Id] FROM [Orders] AS [o]"));
        Assert.Equal(2, SqlColumnCounter.Count("SELECT o.FromDate, o.Id FROM Orders o"));
    }

    [Fact]
    public void Commas_and_keywords_inside_string_literals_do_not_count()
    {
        Assert.Equal(3, SqlColumnCounter.Count("SELECT N'a, b' AS [x], 'FROM' AS [y], [t].[c] FROM [t]"));
        Assert.Equal(1, SqlColumnCounter.Count("SELECT 'it''s, fine' FROM [t]"));
        Assert.Equal(2, SqlColumnCounter.Count("SELECT [weird, name], [t].[from] FROM [t]"));
    }

    [Fact]
    public void Rejects_text_that_is_not_a_query()
    {
        Assert.Throws<ArgumentException>(() => SqlColumnCounter.Count("UPDATE [T] SET [a] = 1"));
        Assert.Throws<ArgumentException>(() => SqlColumnCounter.Count("SELECT 1"));
    }
}
