# Labs

Hands-on exercises on the [Switchback sample](../samples/Switchback/). Each lab is one EF gotcha, its
proper EF fix, and the Slim Domain version, with the numbers to compare. They are also the structure of the
FunkyORM video series: one lab per episode.

Every lab follows the same three steps:

1. **Show the mistake**, and the SQL it sends.
2. **Show the proper EF fix**, and give EF full credit.
3. **Show the Slim Domain version**, and let the reader decide which default they would rather have.

| Lab | Title | Status |
|---|---|---|
| [00](LAB-00-setup-and-tour.md) | Setup and tour | ready |
| [01](LAB-01-36-columns-to-show-5.md) | 36 columns to show 5: the Include chain | ready |
| 02 | N+1 queries: fast on a laptop, slow in production | planned |
| 03 | SQL views that exist only to reshape data | planned |
| 04 | Child counts and totals without loading the children | planned |
| 05 | Paging by a related column, and returning only keys | planned |
| 06 | The accidental delete-all | planned |
| 07 | Change tracking on read-only lists | planned |

The order of labs 02–07 may change. Each one will add a slice to Switchback and a row to its scoreboard.

**Prerequisites for all labs:** the .NET 10 SDK, a SQL Server (LocalDB is enough), and about an hour each.
Lab 00 gets the solution running; do it first.
