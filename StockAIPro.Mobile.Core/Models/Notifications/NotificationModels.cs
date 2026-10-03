using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Notifications;

// Notification Center, preferences, push devices and user reports. All
// notification content is produced by the backend notification engine.

public sealed class NotificationItem
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("type")] public string Type { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("body")] public string Body { get; init; } = "";
    [JsonPropertyName("symbol")] public string? Symbol { get; init; }
    [JsonPropertyName("route")] public string Route { get; init; } = "/notifications";
    [JsonPropertyName("data")] public JsonElement? Data { get; init; }
    [JsonPropertyName("ranking_run_id")] public string? RankingRunId { get; init; }
    [JsonPropertyName("engine_version")] public string? EngineVersion { get; init; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; init; }
    [JsonPropertyName("read")] public bool Read { get; set; }
    [JsonPropertyName("push_status")] public string? PushStatus { get; init; }

    /// <summary>Short human label for the notification type.</summary>
    [JsonIgnore]
    public string TypeLabel => Type switch
    {
        "NEW_TOP_CANDIDATE" => "New Top Candidate",
        "TOP_CANDIDATE_REMOVED" => "Candidate update",
        "SCORE_CHANGE" => "Score change",
        "FQVF_CHANGE" => "FQVF change",
        "WATCHLIST_ALERT" => "Watchlist",
        "DAILY_SUMMARY" => "Daily summary",
        "MARKET_REGIME_CHANGE" => "Market regime",
        "TEST" => "Test",
        _ => Type.Replace('_', ' ').ToLowerInvariant(),
    };
}

public sealed class NotificationList
{
    [JsonPropertyName("items")] public List<NotificationItem> Items { get; init; } = new();
    [JsonPropertyName("total")] public int Total { get; init; }
    [JsonPropertyName("unread")] public int Unread { get; init; }
    [JsonPropertyName("limit")] public int Limit { get; init; }
    [JsonPropertyName("offset")] public int Offset { get; init; }
}

public sealed class UnreadCountResponse
{
    [JsonPropertyName("unread")] public int Unread { get; init; }
}

public sealed class MarkedResponse
{
    [JsonPropertyName("marked")] public int Marked { get; init; }
}

/// <summary>Per-user preferences (backend is the source of truth). Times are
/// HH:MM in India time (Asia/Kolkata).</summary>
public sealed class NotificationPreferences
{
    [JsonPropertyName("push_enabled")] public bool PushEnabled { get; set; } = true;
    [JsonPropertyName("new_top_candidate")] public bool NewTopCandidate { get; set; } = true;
    [JsonPropertyName("top_candidate_removed")] public bool TopCandidateRemoved { get; set; }
    [JsonPropertyName("score_changes")] public bool ScoreChanges { get; set; }
    [JsonPropertyName("fqvf_changes")] public bool FqvfChanges { get; set; }
    [JsonPropertyName("watchlist_alerts")] public bool WatchlistAlerts { get; set; } = true;
    [JsonPropertyName("daily_summary")] public bool DailySummary { get; set; }
    [JsonPropertyName("market_regime")] public bool MarketRegime { get; set; }
    [JsonPropertyName("quiet_hours_enabled")] public bool QuietHoursEnabled { get; set; } = true;
    [JsonPropertyName("quiet_hours_start")] public string QuietHoursStart { get; set; } = "22:00";
    [JsonPropertyName("quiet_hours_end")] public string QuietHoursEnd { get; set; } = "07:00";
    [JsonPropertyName("daily_summary_time")] public string DailySummaryTime { get; set; } = "08:30";

    [JsonPropertyName("timezone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Timezone { get; set; }

    [JsonPropertyName("updated_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UpdatedAt { get; set; }
}

public sealed class DeviceRegistrationRequest
{
    [JsonPropertyName("device_id")] public required string DeviceId { get; init; }
    [JsonPropertyName("platform")] public required string Platform { get; init; }
    [JsonPropertyName("push_token")] public string? PushToken { get; init; }
    [JsonPropertyName("app_version")] public string? AppVersion { get; init; }
    [JsonPropertyName("permission")] public string Permission { get; init; } = "unknown";

    [JsonPropertyName("provider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Provider { get; init; }
}

public sealed class DeviceInfo
{
    [JsonPropertyName("device_id")] public string DeviceId { get; init; } = "";
    [JsonPropertyName("platform")] public string Platform { get; init; } = "";
    [JsonPropertyName("provider")] public string? Provider { get; init; }
    [JsonPropertyName("token")] public string? MaskedToken { get; init; }
    [JsonPropertyName("app_version")] public string? AppVersion { get; init; }
    [JsonPropertyName("permission")] public string? Permission { get; init; }
    [JsonPropertyName("active")] public bool Active { get; init; }
    [JsonPropertyName("last_active_at")] public string? LastActiveAt { get; init; }
}

public static class FeedbackCategories
{
    public const string IncorrectStockData = "INCORRECT_STOCK_DATA";
    public const string StaleData = "STALE_DATA";
    public const string IncorrectCompanyInfo = "INCORRECT_COMPANY_INFO";
    public const string RecommendationIssue = "RECOMMENDATION_ISSUE";
    public const string AppBug = "APP_BUG";
    public const string Other = "OTHER";

    public static readonly IReadOnlyList<(string Key, string Label)> All =
    [
        (IncorrectStockData, "Incorrect stock data"),
        (StaleData, "Stale / outdated data"),
        (IncorrectCompanyInfo, "Incorrect company information"),
        (RecommendationIssue, "Ranking / analysis issue"),
        (AppBug, "App problem"),
        (Other, "Other"),
    ];
}

public sealed class FeedbackRequest
{
    [JsonPropertyName("category")] public required string Category { get; init; }
    [JsonPropertyName("message")] public required string Message { get; init; }

    [JsonPropertyName("symbol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Symbol { get; init; }

    [JsonPropertyName("app_version")] public string? AppVersion { get; init; }
    [JsonPropertyName("platform")] public string? Platform { get; init; }
}

public sealed class FeedbackItem
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("category")] public string Category { get; init; } = "";
    [JsonPropertyName("symbol")] public string? Symbol { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = "";
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("created_at")] public string? CreatedAt { get; init; }
    [JsonPropertyName("admin_note")] public string? AdminNote { get; init; }
}

/// <summary>Result of a report submission: the stored report plus the
/// backend's exact confirmation text (it says the report was recorded for
/// review - never that it was emailed).</summary>
public sealed record FeedbackReceipt(FeedbackItem Item, string Message);
