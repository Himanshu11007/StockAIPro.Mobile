using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Watchlist;

/// <summary>
/// Matches api/schemas_watchlist.py:WatchlistItemResponse exactly, as
/// returned by GET/POST /watchlist. stock_name is always looked up
/// server-side from the stock master (watchlist/service.py) - never trust a
/// client-supplied name pairing.
/// </summary>
public sealed class WatchlistItem
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }

    [JsonPropertyName("stock_name")]
    public required string StockName { get; init; }

    [JsonPropertyName("buy_price")]
    public double BuyPrice { get; init; }

    [JsonPropertyName("buy_date")]
    public required string BuyDate { get; init; }

    [JsonPropertyName("quantity")]
    public double Quantity { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}
