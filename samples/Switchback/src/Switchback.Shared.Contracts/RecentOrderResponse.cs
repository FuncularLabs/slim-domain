namespace Switchback.Shared.Contracts;

/// <summary>
/// The published shape of one row in the Recent orders list. It is an allow-list: internal ids, emails,
/// notes and credit limits have no member here, so they cannot leak. Slim Domain calls this
/// "protected by absence".
/// </summary>
public sealed record RecentOrderResponse(
    string OrderNumber,
    DateTime OrderedUtc,
    string CustomerName,
    string? ShipToCountry,
    decimal Total);
