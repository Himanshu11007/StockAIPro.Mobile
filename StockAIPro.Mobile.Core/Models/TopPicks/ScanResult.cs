using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.TopPicks;

/// <summary>Matches the dict returned by
/// api/services.py:get_scan_result().</summary>
public sealed class ScanResult
{
    [JsonPropertyName("scan_id")]
    public required string ScanId { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("results")]
    public List<TopPickItem> Results { get; init; } = [];
}
