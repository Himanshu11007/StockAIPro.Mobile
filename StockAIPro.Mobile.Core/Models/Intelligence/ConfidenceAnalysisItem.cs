using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>Matches analytics/recommendation_intelligence.py:_confidence_analysis().
/// Successful/Failed are only present when the band actually has data -
/// default to 0 when the backend omits them for an empty band.</summary>
public sealed class ConfidenceAnalysisItem
{
    /// <summary>e.g. "50-60", "90-100" - the exact ML-confidence band
    /// labels the backend defines.</summary>
    [JsonPropertyName("confidence_band")]
    public required string ConfidenceBand { get; init; }

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

    [JsonPropertyName("win_loss_ratio")]
    public double? WinLossRatio { get; init; }
}
