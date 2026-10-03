using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Product;

// Display-only models for the backend's product API (FQVF, StockLens Score,
// Top Investment Candidates, app configuration). The mobile app performs no
// scoring or FQVF calculation - every value here is rendered as delivered.
// Nullable everywhere the backend may legitimately send null (data not
// available), so a missing value is shown as missing, never as zero.

public sealed class AppConfig
{
    [JsonPropertyName("features")] public Dictionary<string, bool> Features { get; init; } = new();
    [JsonPropertyName("disclaimer")] public string? Disclaimer { get; init; }
    [JsonPropertyName("announcement")] public string? Announcement { get; init; }
    [JsonPropertyName("top_picks_limit")] public int TopPicksLimit { get; init; }
    [JsonPropertyName("versions")] public JsonElement? Versions { get; init; }
    [JsonPropertyName("onboarding")] public List<OnboardingPage> Onboarding { get; init; } = new();
    [JsonPropertyName("legal")] public LegalInfo? Legal { get; init; }
}

public sealed class MarketRegimeInfo
{
    [JsonPropertyName("index")] public string? Index { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("regime")] public string? Regime { get; init; }
    [JsonPropertyName("regime_score")] public double? RegimeScore { get; init; }
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("as_of_date")] public string? AsOfDate { get; init; }
    [JsonPropertyName("computed_at")] public string? ComputedAt { get; init; }
}

public sealed class EngineRunInfo
{
    [JsonPropertyName("run_id")] public string RunId { get; init; } = "";
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("finished_at")] public string? FinishedAt { get; init; }
    [JsonPropertyName("stocks_analysed")] public int StocksAnalysed { get; init; }
    [JsonPropertyName("engine_version")] public string? EngineVersion { get; init; }
    [JsonPropertyName("fqvf_version")] public string? FqvfVersion { get; init; }
}

public sealed class CandidateItem
{
    [JsonPropertyName("rank")] public int? Rank { get; init; }
    [JsonPropertyName("symbol")] public string Symbol { get; init; } = "";
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("sector")] public string? Sector { get; init; }
    [JsonPropertyName("industry")] public string? Industry { get; init; }
    [JsonPropertyName("stockai_score")] public double? StockAiScore { get; init; }
    [JsonPropertyName("score_coverage")] public double? ScoreCoverage { get; init; }
    [JsonPropertyName("fqvf_score")] public double? FqvfScore { get; init; }
    [JsonPropertyName("fqvf_summary")] public string? FqvfSummary { get; init; }
    [JsonPropertyName("positives")] public List<string> Positives { get; init; } = new();
    [JsonPropertyName("risks")] public List<string> Risks { get; init; } = new();
    [JsonPropertyName("freshness")] public Dictionary<string, string?> Freshness { get; init; } = new();
    [JsonPropertyName("freshness_status")] public FreshnessStatus? FreshnessStatus { get; init; }
    [JsonPropertyName("labels")] public List<AnalysisLabel> Labels { get; init; } = new();
    [JsonPropertyName("components")] public List<KeyComponent> Components { get; init; } = new();
    [JsonPropertyName("fqvf_counts")] public Dictionary<string, int>? FqvfCounts { get; init; }
    [JsonPropertyName("engine_version")] public string? EngineVersion { get; init; }
    [JsonPropertyName("computed_at")] public string? ComputedAt { get; init; }

    /// <summary>Risk component score (higher = lower volatility/drawdown).</summary>
    [JsonIgnore] public double? RiskScore => Components.FirstOrDefault(c => c.Key == "risk")?.Score;
    [JsonIgnore] public bool IsHighRisk => Labels.Any(l => l.Key == "high_risk");
}

public sealed class TopCandidatesResponse
{
    [JsonPropertyName("items")] public List<CandidateItem> Items { get; init; } = new();
    [JsonPropertyName("total_eligible")] public int TotalEligible { get; init; }
    [JsonPropertyName("limit")] public int Limit { get; init; }
    [JsonPropertyName("run")] public EngineRunInfo? Run { get; init; }
    [JsonPropertyName("market_regime")] public MarketRegimeInfo? MarketRegime { get; init; }
    [JsonPropertyName("disclaimer")] public string? Disclaimer { get; init; }
}

public sealed class ScoreComponent
{
    [JsonPropertyName("key")] public string Key { get; init; } = "";
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("score")] public double? Score { get; init; }
    [JsonPropertyName("weight")] public double Weight { get; init; }
    [JsonPropertyName("contribution")] public double? Contribution { get; init; }
    [JsonPropertyName("basis")] public string? Basis { get; init; }
}

public sealed class RankingInfo
{
    [JsonPropertyName("stockai_score")] public double? StockAiScore { get; init; }
    [JsonPropertyName("score_coverage")] public double? ScoreCoverage { get; init; }
    [JsonPropertyName("rank")] public int? Rank { get; init; }
    [JsonPropertyName("universe_rank")] public UniverseRank? UniverseRank { get; init; }
    [JsonPropertyName("eligible_for_top_picks")] public bool EligibleForTopPicks { get; init; }
    [JsonPropertyName("ineligible_reasons")] public List<string> IneligibleReasons { get; init; } = new();
    [JsonPropertyName("components")] public List<ScoreComponent> Components { get; init; } = new();
    [JsonPropertyName("positives")] public List<string> Positives { get; init; } = new();
    [JsonPropertyName("risks")] public List<string> Risks { get; init; } = new();
    [JsonPropertyName("engine_version")] public string? EngineVersion { get; init; }
    [JsonPropertyName("computed_at")] public string? ComputedAt { get; init; }
}

public sealed class FqvfCheck
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("status")] public string Status { get; init; } = "NOT_AVAILABLE";
    [JsonPropertyName("value")] public JsonElement? Value { get; init; }
    [JsonPropertyName("threshold")] public string? Threshold { get; init; }
    [JsonPropertyName("explanation")] public string? Explanation { get; init; }
    [JsonPropertyName("grade")] public string? Grade { get; init; }
    [JsonPropertyName("scored")] public bool Scored { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
    [JsonPropertyName("source_timestamp")] public string? SourceTimestamp { get; init; }
    [JsonPropertyName("data_available")] public bool DataAvailable { get; init; }

    /// <summary>Formats whatever value shape the backend sent (number,
    /// string, object or null) for display. Never invents a value.</summary>
    [JsonIgnore]
    public string DisplayValue => FormatValue(Value);

    public static string FormatValue(JsonElement? value)
    {
        if (value is not { } v) return "-";
        switch (v.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return "-";
            case JsonValueKind.Number:
                return v.GetDouble().ToString("0.####", CultureInfo.InvariantCulture);
            case JsonValueKind.String:
                return v.GetString() ?? "-";
            case JsonValueKind.True:
            case JsonValueKind.False:
                return v.GetBoolean() ? "yes" : "no";
            case JsonValueKind.Object:
                var sb = new StringBuilder();
                foreach (var p in v.EnumerateObject())
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(p.Name.Replace('_', ' ')).Append(": ").Append(FormatValue(p.Value));
                }
                return sb.Length == 0 ? "-" : sb.ToString();
            default:
                return v.ToString();
        }
    }
}

public sealed class FqvfResult
{
    [JsonPropertyName("version")] public string? Version { get; init; }
    [JsonPropertyName("calculated_at")] public string? CalculatedAt { get; init; }
    [JsonPropertyName("checks")] public List<FqvfCheck> Checks { get; init; } = new();
    [JsonPropertyName("counts")] public Dictionary<string, int> Counts { get; init; } = new();
    [JsonPropertyName("score")] public double? Score { get; init; }
    [JsonPropertyName("coverage")] public double Coverage { get; init; }
    [JsonPropertyName("summary")] public string? Summary { get; init; }
}

public sealed class TechnicalInfo
{
    [JsonPropertyName("return_20d")] public double? Return20d { get; init; }
    [JsonPropertyName("return_60d")] public double? Return60d { get; init; }
    [JsonPropertyName("return_250d")] public double? Return250d { get; init; }
    [JsonPropertyName("volatility_annual")] public double? VolatilityAnnual { get; init; }
    [JsonPropertyName("max_drawdown_1y")] public double? MaxDrawdown1y { get; init; }
    [JsonPropertyName("rsi")] public double? Rsi { get; init; }
    [JsonPropertyName("trend_score")] public double? TrendScore { get; init; }
    [JsonPropertyName("trend_daily")] public TrendInfo? TrendDaily { get; init; }
    [JsonPropertyName("trend_weekly")] public TrendInfo? TrendWeekly { get; init; }
    [JsonPropertyName("avg_volume_20d")] public double? AvgVolume20d { get; init; }
    [JsonPropertyName("regime")] public string? Regime { get; init; }
    [JsonPropertyName("regime_reason")] public string? RegimeReason { get; init; }
}

public sealed class MarketInfo
{
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("as_of_date")] public string? AsOfDate { get; init; }
    [JsonPropertyName("close")] public double? Close { get; init; }
    [JsonPropertyName("issues")] public List<string> Issues { get; init; } = new();
    [JsonPropertyName("technical")] public TechnicalInfo? Technical { get; init; }
}

public sealed class MlSignalInfo
{
    [JsonPropertyName("available")] public bool Available { get; init; }
    [JsonPropertyName("direction")] public string? Direction { get; init; }
    [JsonPropertyName("probability_up")] public double? ProbabilityUp { get; init; }
    [JsonPropertyName("prediction_bar_date")] public string? PredictionBarDate { get; init; }
    [JsonPropertyName("disclaimer")] public string? Disclaimer { get; init; }
    [JsonPropertyName("reason")] public string? Reason { get; init; }
}

public sealed class EngineInfo
{
    [JsonPropertyName("ranking")] public string? Ranking { get; init; }
    [JsonPropertyName("fqvf")] public string? Fqvf { get; init; }
    [JsonPropertyName("run_id")] public string? RunId { get; init; }
    [JsonPropertyName("run_kind")] public string? RunKind { get; init; }
}

public sealed class StockAnalysis
{
    [JsonPropertyName("symbol")] public string Symbol { get; init; } = "";
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("exchange")] public string? Exchange { get; init; }
    [JsonPropertyName("sector")] public string? Sector { get; init; }
    [JsonPropertyName("industry")] public string? Industry { get; init; }
    [JsonPropertyName("data_status")] public string? DataStatus { get; init; }
    [JsonPropertyName("data_status_reason")] public string? DataStatusReason { get; init; }
    [JsonPropertyName("ranking")] public RankingInfo Ranking { get; init; } = new();
    [JsonPropertyName("fqvf")] public FqvfResult Fqvf { get; init; } = new();
    [JsonPropertyName("market")] public MarketInfo? Market { get; init; }
    [JsonPropertyName("ml_signal")] public MlSignalInfo? MlSignal { get; init; }
    [JsonPropertyName("freshness")] public Dictionary<string, string?> Freshness { get; init; } = new();
    [JsonPropertyName("freshness_status")] public FreshnessStatus? FreshnessStatus { get; init; }
    [JsonPropertyName("labels")] public List<AnalysisLabel> Labels { get; init; } = new();
    [JsonPropertyName("explanation")] public RankExplanation? Explanation { get; init; }
    [JsonPropertyName("market_regime")] public MarketRegimeInfo? MarketRegime { get; init; }
    [JsonPropertyName("reference_price")] public ReferencePrice? ReferencePrice { get; init; }
    [JsonPropertyName("engine")] public EngineInfo? Engine { get; init; }
}
