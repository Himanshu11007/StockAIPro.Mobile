using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>
/// Matches analytics/recommendation_intelligence.py:_generate_recommendations()
/// - a deterministic, evidence-based observation for developers. Never
/// suggests changing weights/thresholds itself; purely descriptive.
///
/// Confidence here is the engine's own 0-100 confidence in THIS
/// observation being noteworthy (e.g. "how much evidence backs this
/// insight") - a completely different concept from AnalyzeStockResult's ML
/// prediction confidence. Do not conflate the two in the UI.
/// </summary>
public sealed class DeveloperRecommendation
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>"Low" | "Medium" | "High".</summary>
    [JsonPropertyName("priority")]
    public required string Priority { get; init; }

    [JsonPropertyName("confidence")]
    public int Confidence { get; init; }

    [JsonPropertyName("recommendation")]
    public required string Recommendation { get; init; }

    [JsonPropertyName("evidence")]
    public required string Evidence { get; init; }
}
