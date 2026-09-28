using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.TopPicks;

/// <summary>Matches the dict returned by
/// api/services.py:get_scan_status().</summary>
public sealed class ScanStatus
{
    [JsonPropertyName("scan_id")]
    public required string ScanId { get; init; }

    /// <summary>"running" | "completed" | "failed" | "not_found".</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("progress")]
    public int Progress { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
