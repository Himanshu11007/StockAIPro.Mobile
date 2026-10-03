using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Product;

// Additional display models: analytical labels, data freshness, the "why is
// this stock ranked here" explanation, market status, onboarding/legal
// configuration, and the Performance / AI Intelligence overviews. All values
// come from the backend and are rendered as delivered.

/// <summary>Analytical label ("Top Candidate", "Strong Quality", "High Risk",
/// "Needs Review", ...). Tone: positive / negative / warning.</summary>
public sealed class AnalysisLabel
{
    [JsonPropertyName("key")] public string Key { get; init; } = "";
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("tone")] public string Tone { get; init; } = "neutral";
}

public sealed class KeyComponent
{
    [JsonPropertyName("key")] public string Key { get; init; } = "";
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("score")] public double? Score { get; init; }
}

/// <summary>Backend freshness verdict: OK / STALE / UNAVAILABLE with the
/// exact timestamps and human-readable issues.</summary>
public sealed class FreshnessStatus
{
    [JsonPropertyName("status")] public string Status { get; init; } = "UNAVAILABLE";
    [JsonPropertyName("market_data_as_of")] public string? MarketDataAsOf { get; init; }
    [JsonPropertyName("market_data_age_days")] public int? MarketDataAgeDays { get; init; }
    [JsonPropertyName("analysis_computed_at")] public string? AnalysisComputedAt { get; init; }
    [JsonPropertyName("analysis_age_days")] public int? AnalysisAgeDays { get; init; }
    [JsonPropertyName("fundamentals_fetched_at")] public string? FundamentalsFetchedAt { get; init; }
    [JsonPropertyName("fiscal_period_end")] public string? FiscalPeriodEnd { get; init; }
    [JsonPropertyName("issues")] public List<string> Issues { get; init; } = new();

    [JsonIgnore] public bool IsStale => Status == "STALE";
    [JsonIgnore] public bool IsUnavailable => Status == "UNAVAILABLE";
}

public sealed class RankExplanation
{
    [JsonPropertyName("summary")] public string Summary { get; init; } = "";
    [JsonPropertyName("strengths")] public List<string> Strengths { get; init; } = new();
    [JsonPropertyName("weaknesses")] public List<string> Weaknesses { get; init; } = new();
    [JsonPropertyName("missing")] public List<string> Missing { get; init; } = new();
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed class UniverseRank
{
    [JsonPropertyName("rank")] public int? Rank { get; init; }
    [JsonPropertyName("eligible_total")] public int? EligibleTotal { get; init; }
    [JsonPropertyName("stockai_score")] public double? StockAiScore { get; init; }
    [JsonPropertyName("run_id")] public string? RunId { get; init; }
    [JsonPropertyName("computed_at")] public string? ComputedAt { get; init; }
}

public sealed class ReferencePrice
{
    [JsonPropertyName("close")] public double? Close { get; init; }
    [JsonPropertyName("as_of_date")] public string? AsOfDate { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed class OnboardingPage
{
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("body")] public string Body { get; init; } = "";
}

public sealed class LegalInfo
{
    [JsonPropertyName("privacy_url")] public string? PrivacyUrl { get; init; }
    [JsonPropertyName("terms_url")] public string? TermsUrl { get; init; }
    [JsonPropertyName("support_email")] public string? SupportEmail { get; init; }
    [JsonPropertyName("privacy_summary")] public string? PrivacySummary { get; init; }
}

/// <summary>GET /market/status — NSE session status in IST.</summary>
public sealed class MarketStatusInfo
{
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("is_open")] public bool IsOpen { get; init; }
    [JsonPropertyName("exchange")] public string? Exchange { get; init; }
    [JsonPropertyName("timezone")] public string? Timezone { get; init; }
    [JsonPropertyName("now_ist")] public string? NowIst { get; init; }
    [JsonPropertyName("date")] public string? Date { get; init; }
    [JsonPropertyName("session_hours")] public string? SessionHours { get; init; }
    [JsonPropertyName("next_trading_day")] public string? NextTradingDay { get; init; }
    [JsonPropertyName("holiday_calendar_configured")] public bool HolidayCalendarConfigured { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
}

// ── Performance overview (GET /performance/overview) ────────────────────────

public sealed class PerformanceOverview
{
    [JsonPropertyName("sections")] public PerformanceSections Sections { get; init; } = new();
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed class PerformanceSections
{
    [JsonPropertyName("ranking_v1_prospective")] public ProspectiveSection? Prospective { get; init; }
    [JsonPropertyName("ranking_v1_validation")] public ValidationSection? Validation { get; init; }
    [JsonPropertyName("post_fix_signals")] public SignalSection? PostFixSignals { get; init; }
    [JsonPropertyName("legacy_signals")] public SignalSection? LegacySignals { get; init; }
}

public sealed class ProspectiveSection
{
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("methodology")] public string? Methodology { get; init; }
    [JsonPropertyName("benchmark")] public string? Benchmark { get; init; }
    [JsonPropertyName("ranking_runs_tracked")] public int RankingRunsTracked { get; init; }
    [JsonPropertyName("stock_snapshots")] public int StockSnapshots { get; init; }
    [JsonPropertyName("first_snapshot")] public string? FirstSnapshot { get; init; }
    [JsonPropertyName("latest_snapshot")] public string? LatestSnapshot { get; init; }
    [JsonPropertyName("outcomes_recorded")] public int OutcomesRecorded { get; init; }
    [JsonPropertyName("performance")] public List<ProspectiveRow> Performance { get; init; } = new();
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("limitations")] public List<string> Limitations { get; init; } = new();
}

public sealed class ProspectiveRow
{
    [JsonPropertyName("horizon")] public string Horizon { get; init; } = "";
    [JsonPropertyName("portfolio")] public string Portfolio { get; init; } = "";
    [JsonPropertyName("runs")] public int Runs { get; init; }
    [JsonPropertyName("stock_outcomes")] public int StockOutcomes { get; init; }
    [JsonPropertyName("mean_return")] public double? MeanReturn { get; init; }
    [JsonPropertyName("mean_excess_vs_nifty")] public double? MeanExcessVsNifty { get; init; }
    [JsonPropertyName("mean_excess_vs_eligible_universe")] public double? MeanExcessVsUniverse { get; init; }
    [JsonPropertyName("hit_rate_vs_nifty")] public double? HitRateVsNifty { get; init; }
}

public sealed class ValidationSection
{
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("methodology")] public string? Methodology { get; init; }
    [JsonPropertyName("benchmark")] public string? Benchmark { get; init; }
    [JsonPropertyName("horizon")] public string? Horizon { get; init; }
    [JsonPropertyName("results")] public List<MetricValue> Results { get; init; } = new();
    [JsonPropertyName("conclusion")] public string? Conclusion { get; init; }
    [JsonPropertyName("limitations")] public List<string> Limitations { get; init; } = new();
}

public sealed class MetricValue
{
    [JsonPropertyName("metric")] public string Metric { get; init; } = "";
    [JsonPropertyName("value")] public string Value { get; init; } = "";
}

public sealed class SignalSection
{
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("methodology")] public string? Methodology { get; init; }
    [JsonPropertyName("count")] public int Count { get; init; }
    [JsonPropertyName("period_start")] public string? PeriodStart { get; init; }
    [JsonPropertyName("period_end")] public string? PeriodEnd { get; init; }
    [JsonPropertyName("horizon")] public string? Horizon { get; init; }
    [JsonPropertyName("by_signal")] public List<SignalGroup> BySignal { get; init; } = new();
    [JsonPropertyName("success_definition")] public string? SuccessDefinition { get; init; }
    [JsonPropertyName("caveat")] public string? Caveat { get; init; }
}

public sealed class SignalGroup
{
    [JsonPropertyName("signal")] public string Signal { get; init; } = "";
    [JsonPropertyName("count")] public int Count { get; init; }
    [JsonPropertyName("success_rate")] public double? SuccessRate { get; init; }
    [JsonPropertyName("avg_return_pct")] public double? AvgReturnPct { get; init; }
}

// ── AI Intelligence overview (GET /intelligence/overview) ───────────────────

public sealed class IntelligenceOverview
{
    [JsonPropertyName("sections")] public List<IntelligenceSection> Sections { get; init; } = new();
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed class IntelligenceSection
{
    [JsonPropertyName("key")] public string Key { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("text")] public string? Text { get; init; }
    [JsonPropertyName("regime")] public MarketRegimeInfo? Regime { get; init; }
    [JsonPropertyName("market_status")] public MarketStatusInfo? MarketStatus { get; init; }
    [JsonPropertyName("displayed")] public bool? Displayed { get; init; }
}

public sealed class TrendInfo
{
    [JsonPropertyName("trend")] public string? Trend { get; init; }
    [JsonPropertyName("score")] public double? Score { get; init; }
}
