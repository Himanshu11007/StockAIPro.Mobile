using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Analysis;

/// <summary>
/// Structured, human-readable explanation for a recommendation - matches
/// api/schemas.py:RecommendationExplanation exactly. Built by
/// utils.explainability.build_recommendation_explanation(); purely
/// presentational, never influences the signal/score itself.
/// </summary>
public sealed class RecommendationExplanation
{
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("signal_explanation")]
    public required string SignalExplanation { get; init; }

    [JsonPropertyName("strengths")]
    public List<string> Strengths { get; init; } = [];

    [JsonPropertyName("weaknesses")]
    public List<string> Weaknesses { get; init; } = [];

    [JsonPropertyName("watch_points")]
    public List<string> WatchPoints { get; init; } = [];

    [JsonPropertyName("pillar_breakdown")]
    public List<PillarBreakdown> PillarBreakdown { get; init; } = [];

    [JsonPropertyName("risk_summary")]
    public required string RiskSummary { get; init; }

    [JsonPropertyName("confidence_note")]
    public string ConfidenceNote { get; init; } = "";

    [JsonPropertyName("final_interpretation")]
    public required string FinalInterpretation { get; init; }
}
