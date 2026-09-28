using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Performance;

/// <summary>Matches one row of GET /performance/by-confidence
/// (storage/performance_analytics.py:confidence_performance()) - see
/// SignalPerformance for why the JSON keys look like this.</summary>
public sealed class ConfidencePerformance
{
    /// <summary>e.g. "50–60%", "90%+" - the exact band labels the backend
    /// defines; do not recompute or re-bucket these client-side.</summary>
    [JsonPropertyName("Confidence Band")]
    public required string ConfidenceBand { get; init; }

    [JsonPropertyName("Count")]
    public int Count { get; init; }

    [JsonPropertyName("Success Rate %")]
    public double SuccessRatePercent { get; init; }

    [JsonPropertyName("Avg Return %")]
    public double AvgReturnPercent { get; init; }
}
