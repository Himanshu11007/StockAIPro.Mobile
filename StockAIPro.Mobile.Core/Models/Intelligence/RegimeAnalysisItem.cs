using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>Matches analytics/recommendation_intelligence.py:_regime_analysis().
/// Note is only present in the fallback "market regime not yet stored" row.</summary>
public sealed class RegimeAnalysisItem
{
    [JsonPropertyName("regime")]
    public required string Regime { get; init; }

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

    [JsonPropertyName("note")]
    public string? Note { get; init; }
}
