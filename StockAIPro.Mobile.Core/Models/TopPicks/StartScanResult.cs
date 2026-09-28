using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.TopPicks;

/// <summary>Matches the dict returned by
/// api/services.py:start_top_picks_scan() - the "data" payload of
/// POST /top-picks/start.</summary>
public sealed class StartScanResult
{
    [JsonPropertyName("scan_id")]
    public required string ScanId { get; init; }

    /// <summary>Always "started" - see start_top_picks_scan(). The
    /// underlying scanner may already have been running for a different
    /// caller; that is not distinguished here.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }
}
