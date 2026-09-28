using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>
/// Matches analytics/recommendation_intelligence.py:generate_engine_report()
/// exactly, as returned by GET /intelligence/report. Read-only analytics -
/// never modifies weights, thresholds, or the database. If Meta.Error is
/// set, the engine failed internally and every list here will be empty.
/// </summary>
public sealed class IntelligenceReport
{
    [JsonPropertyName("summary")]
    public IntelligenceSummary Summary { get; init; } = new();

    [JsonPropertyName("threshold_analysis")]
    public List<ThresholdAnalysisItem> ThresholdAnalysis { get; init; } = [];

    [JsonPropertyName("confidence_analysis")]
    public List<ConfidenceAnalysisItem> ConfidenceAnalysis { get; init; } = [];

    [JsonPropertyName("pillar_analysis")]
    public List<PillarAnalysisItem> PillarAnalysis { get; init; } = [];

    [JsonPropertyName("sector_analysis")]
    public List<SectorAnalysisItem> SectorAnalysis { get; init; } = [];

    [JsonPropertyName("regime_analysis")]
    public List<RegimeAnalysisItem> RegimeAnalysis { get; init; } = [];

    [JsonPropertyName("signal_analysis")]
    public List<SignalAnalysisItem> SignalAnalysis { get; init; } = [];

    [JsonPropertyName("recommendations")]
    public List<DeveloperRecommendation> Recommendations { get; init; } = [];

    [JsonPropertyName("meta")]
    public required IntelligenceMeta Meta { get; init; }
}
