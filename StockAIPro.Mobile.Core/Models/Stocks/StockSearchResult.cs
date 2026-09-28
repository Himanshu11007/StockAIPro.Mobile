using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Stocks;

/// <summary>
/// Matches api/schemas_stocks.py:StockSearchResultResponse exactly, as
/// returned by GET /stocks and GET /stocks/{symbol}. Always an active stock
/// - the backend never returns an inactive one from this endpoint (see
/// stocks/service.py), so there is no "active" field to check here.
/// </summary>
public sealed class StockSearchResult
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("sector")]
    public string? Sector { get; init; }

    [JsonPropertyName("industry")]
    public string? Industry { get; init; }

    [JsonPropertyName("exchange")]
    public required string Exchange { get; init; }

    [JsonPropertyName("analysis_enabled")]
    public bool AnalysisEnabled { get; init; }
}
