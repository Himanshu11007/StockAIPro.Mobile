using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>
/// Matches analytics/recommendation_intelligence.py:_threshold_analysis() -
/// performance of recommendations whose confluence_score >= Threshold.
/// Read-only observation; the backend's actual thresholds are never changed
/// based on this data automatically.
/// </summary>
public sealed class ThresholdAnalysisItem
{
    [JsonPropertyName("threshold")]
    public double Threshold { get; init; }

    [JsonPropertyName("trades")]
    public int Trades { get; init; }

    [JsonPropertyName("successful")]
    public int Successful { get; init; }

    [JsonPropertyName("failed")]
    public int Failed { get; init; }

    [JsonPropertyName("success_rate")]
    public double SuccessRate { get; init; }

    [JsonPropertyName("avg_return")]
    public double AvgReturn { get; init; }

    [JsonPropertyName("median_return")]
    public double MedianReturn { get; init; }

    /// <summary>Null when there are no failed trades in this bucket to
    /// divide by (see _stats()'s own None-over-Infinity handling) or when
    /// the bucket is empty.</summary>
    [JsonPropertyName("win_loss_ratio")]
    public double? WinLossRatio { get; init; }
}
