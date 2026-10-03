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

    /// <summary>Optional personal note: a watchlist entry follows a stock's
    /// analysis; a purchase price/date is not required.</summary>
    [JsonPropertyName("buy_price")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? BuyPrice { get; init; }

    [JsonPropertyName("buy_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BuyDate { get; init; }

    [JsonPropertyName("quantity")]
    public double Quantity { get; init; } = 1;
}
