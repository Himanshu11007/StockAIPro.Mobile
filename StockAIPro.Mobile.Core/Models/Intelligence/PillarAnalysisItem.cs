using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>
/// Matches analytics/recommendation_intelligence.py:_pillar_analysis() -
/// one of the 8 explainability pillars (e.g. "ML Direction", "News
/// Sentiment") and how strongly it correlates with recommendation success.
/// AvgScore/CorrWithSuccess/CorrWithReturn are all null when the backend
/// has no stored data yet for this pillar (Interpretation explains why).
/// </summary>
public sealed class PillarAnalysisItem
{
    [JsonPropertyName("pillar")]
    public required string Pillar { get; init; }

    [JsonPropertyName("avg_score")]
    public double? AvgScore { get; init; }

    [JsonPropertyName("corr_with_success")]
    public double? CorrWithSuccess { get; init; }

    [JsonPropertyName("corr_with_return")]
    public double? CorrWithReturn { get; init; }

    [JsonPropertyName("interpretation")]
    public required string Interpretation { get; init; }
}
