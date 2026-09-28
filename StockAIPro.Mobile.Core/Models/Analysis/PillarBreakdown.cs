using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Analysis;

/// <summary>One row of the explainability pillar table - matches
/// api/schemas.py:PillarBreakdown exactly (utils/explainability.py).</summary>
public sealed class PillarBreakdown
{
    [JsonPropertyName("pillar")]
    public required string Pillar { get; init; }

    [JsonPropertyName("score")]
    public double Score { get; init; }

    /// <summary>"positive" | "neutral" | "negative"</summary>
    [JsonPropertyName("impact")]
    public required string Impact { get; init; }

    [JsonPropertyName("weight")]
    public double Weight { get; init; }

    [JsonPropertyName("weighted_contribution")]
    public double WeightedContribution { get; init; }

    [JsonPropertyName("explanation")]
    public required string Explanation { get; init; }
}
