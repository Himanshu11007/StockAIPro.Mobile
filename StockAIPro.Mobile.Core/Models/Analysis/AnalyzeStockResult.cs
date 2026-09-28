using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Analysis;

/// <summary>
/// Matches the dict api/services.py:analyze_stock() actually returns
/// (identical field set to api/schemas.py:AnalyzeStockResponse, which is
/// documentation-only here - the route doesn't set it as response_model).
///
/// These four numeric fields look similar but are distinct concepts from
/// the backend's decision engine - never conflate them in the UI:
///   Score      = confluence score (utils/decision_engine.py), 0-1.
///   Confidence = the ML model's prediction confidence, 0-100.
///   Accuracy   = the trained model's own backtested accuracy, 0-1
///                (rounded to 4dp in services.py - not a percentage here).
///   NewsScore  = news sentiment score, roughly -1..1.
/// </summary>
public sealed class AnalyzeStockResult
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }

    [JsonPropertyName("stock")]
    public required string Stock { get; init; }

    /// <summary>e.g. "STRONG BUY" | "BUY" | "HOLD" | "SELL" | "STRONG SELL".</summary>
    [JsonPropertyName("signal")]
    public required string Signal { get; init; }

    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("accuracy")]
    public double Accuracy { get; init; }

    [JsonPropertyName("news_score")]
    public double NewsScore { get; init; }

    [JsonPropertyName("regime")]
    public required string Regime { get; init; }

    [JsonPropertyName("weekly_trend")]
    public required string WeeklyTrend { get; init; }

    [JsonPropertyName("daily_trend")]
    public required string DailyTrend { get; init; }

    [JsonPropertyName("target")]
    public double? Target { get; init; }

    [JsonPropertyName("stop_loss")]
    public double? StopLoss { get; init; }

    [JsonPropertyName("factors")]
    public List<string> Factors { get; init; } = [];

    [JsonPropertyName("explanation")]
    public RecommendationExplanation? Explanation { get; init; }
}
