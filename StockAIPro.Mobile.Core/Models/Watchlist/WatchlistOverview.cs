using System.Text.Json.Serialization;
using StockAIPro.Mobile.Models.Product;

namespace StockAIPro.Mobile.Models.Watchlist;

/// <summary>GET /watchlist/overview: each watched stock with its latest
/// analysis. Null means "not available", never zero.</summary>
public sealed class WatchlistOverview
{
    [JsonPropertyName("items")] public List<WatchlistOverviewItem> Items { get; init; } = new();
}

public sealed class WatchlistOverviewItem
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("symbol")] public string Symbol { get; init; } = "";
    [JsonPropertyName("stock_name")] public string StockName { get; init; } = "";
    [JsonPropertyName("sector")] public string? Sector { get; init; }
    [JsonPropertyName("buy_price")] public double? BuyPrice { get; init; }
    [JsonPropertyName("buy_date")] public string? BuyDate { get; init; }
    [JsonPropertyName("analysed")] public bool Analysed { get; init; }
    [JsonPropertyName("stockai_score")] public double? StockAiScore { get; init; }
    [JsonPropertyName("rank")] public int? Rank { get; init; }
    [JsonPropertyName("eligible")] public bool? Eligible { get; init; }
    [JsonPropertyName("fqvf_score")] public double? FqvfScore { get; init; }
    [JsonPropertyName("fqvf_passed")] public int? FqvfPassed { get; init; }
    [JsonPropertyName("score_change")] public double? ScoreChange { get; init; }
    [JsonPropertyName("previous_score")] public double? PreviousScore { get; init; }
    [JsonPropertyName("price")] public double? Price { get; init; }
    [JsonPropertyName("price_as_of")] public string? PriceAsOf { get; init; }
    [JsonPropertyName("analysis_computed_at")] public string? AnalysisComputedAt { get; init; }
    [JsonPropertyName("freshness_status")] public FreshnessStatus? FreshnessStatus { get; init; }
    [JsonPropertyName("labels")] public List<AnalysisLabel> Labels { get; init; } = new();
    [JsonPropertyName("alerts")] public WatchlistAlerts Alerts { get; set; } = new();
    [JsonPropertyName("alerts_active")] public bool AlertsActive { get; init; }
}

/// <summary>Per-stock watchlist alert switches (PUT /watchlist/{id}/alerts).</summary>
public sealed class WatchlistAlerts
{
    [JsonPropertyName("score_changes")] public bool ScoreChanges { get; set; } = true;
    [JsonPropertyName("rank_changes")] public bool RankChanges { get; set; } = true;
    [JsonPropertyName("fqvf_changes")] public bool FqvfChanges { get; set; } = true;
    [JsonPropertyName("status_changes")] public bool StatusChanges { get; set; } = true;
    [JsonPropertyName("muted")] public bool Muted { get; set; }
}
