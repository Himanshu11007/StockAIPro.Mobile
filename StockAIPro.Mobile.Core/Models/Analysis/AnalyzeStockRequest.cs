using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Analysis;

public sealed class AnalyzeStockRequest
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }
}
