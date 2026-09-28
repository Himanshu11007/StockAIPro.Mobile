using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.TopPicks;

/// <summary>
/// Matches one element of the "results" list from GET
/// /top-picks/result/{scan_id} - the exact dict scanner/engine.py's
/// per-stock scan function returns (cached to disk by
/// scanner/cache.py:save_category_cache and read back unchanged; verified
/// field-by-field against scanner/engine.py, not against the aspirational
/// api/schemas.py:TopPickItem, which the route does not actually use as a
/// response_model).
///
/// Important: Accuracy here is on a 0-100 scale (scanner/engine.py rounds
/// acc*100), UNLIKE AnalyzeStockResult.Accuracy, which is 0-1. Do not
/// compare the two directly without converting.
/// </summary>
public sealed class TopPickItem
{
    [JsonPropertyName("stock")]
    public required string Stock { get; init; }

    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }

    [JsonPropertyName("signal")]
    public required string Signal { get; init; }

    /// <summary>Confluence score, 0-1.</summary>
    [JsonPropertyName("score")]
    public double Score { get; init; }

    /// <summary>ML prediction confidence, 0-100.</summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>Backtested model accuracy, 0-100 (not 0-1 - see class doc).</summary>
    [JsonPropertyName("accuracy")]
    public double Accuracy { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("factors")]
    public List<string> Factors { get; init; } = [];

    [JsonPropertyName("close")]
    public double Close { get; init; }

    [JsonPropertyName("stop_loss")]
    public double? StopLoss { get; init; }

    [JsonPropertyName("target")]
    public double? Target { get; init; }

    [JsonPropertyName("rr_ratio")]
    public double? RrRatio { get; init; }

    [JsonPropertyName("regime")]
    public required string Regime { get; init; }

    [JsonPropertyName("weekly_trend")]
    public required string WeeklyTrend { get; init; }

    [JsonPropertyName("daily_trend")]
    public required string DailyTrend { get; init; }

    [JsonPropertyName("timeframe_score")]
    public double TimeframeScore { get; init; }

    /// <summary>Always "Ensemble" today - see scanner/engine.py.</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("news_score")]
    public double NewsScore { get; init; }

    /// <summary>The 8 pillar diagnostic scores, keyed by human-readable
    /// pillar label (e.g. "ML Direction", "Technical Analysis") - see
    /// utils/explainability.py:compute_pillar_scores().</summary>
    [JsonPropertyName("pillar_scores")]
    public Dictionary<string, double> PillarScores { get; init; } = [];

    [JsonPropertyName("weighted_score")]
    public double WeightedScore { get; init; }

    [JsonPropertyName("sector")]
    public string? Sector { get; init; }

    [JsonPropertyName("engine_version")]
    public required string EngineVersion { get; init; }
}
