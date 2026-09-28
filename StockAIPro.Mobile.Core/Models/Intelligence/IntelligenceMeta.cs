using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Intelligence;

/// <summary>Matches the "meta" block of
/// analytics/recommendation_intelligence.py:generate_engine_report(). Error
/// is only present if the engine itself failed internally (the fallback
/// path) - when non-null, the rest of the report's lists will be empty and
/// Summary will be an empty/default object.</summary>
public sealed class IntelligenceMeta
{
    [JsonPropertyName("execution_time_ms")]
    public double ExecutionTimeMs { get; init; }

    [JsonPropertyName("records_analyzed")]
    public int RecordsAnalyzed { get; init; }

    [JsonPropertyName("engine_version")]
    public required string EngineVersion { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
