using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Watchlist;

/// <summary>
/// Matches api/schemas_watchlist.py:WatchlistAddRequest exactly. There is no
/// user_id field - ownership is always derived from the authenticated
/// user's access token server-side (see watchlist/service.py); the client
/// must never send one.
/// </summary>
public sealed class WatchlistAddRequest
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }

    [JsonPropertyName("buy_price")]
    public required double BuyPrice { get; init; }

    [JsonPropertyName("buy_date")]
    public required string BuyDate { get; init; }

    [JsonPropertyName("quantity")]
    public double Quantity { get; init; } = 1;
}
