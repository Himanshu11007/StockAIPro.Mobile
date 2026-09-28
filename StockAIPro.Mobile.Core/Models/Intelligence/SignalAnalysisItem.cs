using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>Matches analytics/recommendation_intelligence.py:_signal_analysis().</summary>
public sealed class SignalAnalysisItem
{
    /// <summary>e.g. "STRONG BUY" | "BUY" | "HOLD" | "SELL" | "STRONG SELL".</summary>
    [JsonPropertyName("signal")]
    public required string Signal { get; init; }

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
