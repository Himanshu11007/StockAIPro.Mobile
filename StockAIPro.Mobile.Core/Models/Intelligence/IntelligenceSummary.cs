using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>Matches analytics/recommendation_intelligence.py:_summary().
/// Historical/observed figures over validated recommendations only - never
/// present as a future guarantee.
///
/// No field here is `required`: if the intelligence engine itself throws
/// (see generate_engine_report()'s except branch), "summary" comes back as
/// a literal empty JSON object {} - deserializing that must not throw, so
/// every property has a safe default instead.</summary>
public sealed class IntelligenceSummary
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

    [JsonPropertyName("median_return")]
    public double MedianReturn { get; init; }

    [JsonPropertyName("data_quality")]
    public string DataQuality { get; init; } = "";
}
