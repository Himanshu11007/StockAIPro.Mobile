using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Performance;

/// <summary>Matches one row of GET /performance/by-confluence
/// (storage/performance_analytics.py:confluence_performance()) - see
/// SignalPerformance for why the JSON keys look like this.</summary>
public sealed class ConfluencePerformance
{
    /// <summary>e.g. "&lt;0.50", "0.80+" - the exact band labels the backend
    /// defines; do not recompute or re-bucket these client-side.</summary>
    [JsonPropertyName("Confluence Band")]
    public required string ConfluenceBand { get; init; }

    [JsonPropertyName("Count")]
    public int Count { get; init; }

    [JsonPropertyName("Success Rate %")]
    public double SuccessRatePercent { get; init; }

    [JsonPropertyName("Avg Return %")]
    public double AvgReturnPercent { get; init; }
}
