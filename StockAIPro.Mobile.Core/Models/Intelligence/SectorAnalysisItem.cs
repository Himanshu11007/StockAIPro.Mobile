using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>Matches analytics/recommendation_intelligence.py:_sector_analysis().
/// Note (a data-availability caveat) is only present in the fallback
/// "no sector data yet" row; BestStock/WorstStock can be null even with
/// real data if no recommendation in the sector has a return value yet.</summary>
public sealed class SectorAnalysisItem
{
    [JsonPropertyName("sector")]
    public required string Sector { get; init; }

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

    [JsonPropertyName("best_stock")]
    public string? BestStock { get; init; }

    [JsonPropertyName("worst_stock")]
    public string? WorstStock { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }
}
