using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Performance;

/// <summary>
/// Matches api/schemas.py:PerformanceSummaryResponse / the dict
/// storage/performance_analytics.py:summary_metrics() returns, as returned
/// by GET /performance/summary. These are historical, observed outcomes
/// over validated recommendations - never present them as a guarantee of
/// future performance.
/// </summary>
public sealed class PerformanceSummary
{
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("successful")]
    public int Successful { get; init; }

    [JsonPropertyName("failed")]
    public int Failed { get; init; }

    [JsonPropertyName("success_rate")]
    public double SuccessRate { get; init; }

    [JsonPropertyName("avg_return")]
    public double AvgReturn { get; init; }

    [JsonPropertyName("best_return")]
    public double BestReturn { get; init; }

    [JsonPropertyName("worst_return")]
    public double WorstReturn { get; init; }
}
