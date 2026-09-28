using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.TopPicks;

public sealed class StartScanRequest
{
    [JsonPropertyName("category")]
    public required string Category { get; init; }
}
