using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Performance;

/// <summary>
/// Matches one row of GET /performance/by-signal - a pandas DataFrame
/// serialized straight to JSON (storage/performance_analytics.py:
/// signal_performance()), which is why the JSON keys have spaces and a
/// literal "%" rather than the snake_case used elsewhere in this API.
/// </summary>
public sealed class SignalPerformance
{
    /// <summary>e.g. "STRONG BUY" | "BUY" | "HOLD" | "SELL" | "STRONG SELL".</summary>
    [JsonPropertyName("Signal")]
    public required string Signal { get; init; }

    [JsonPropertyName("Count")]
    public int Count { get; init; }

    [JsonPropertyName("Success Rate %")]
    public double SuccessRatePercent { get; init; }

    [JsonPropertyName("Avg Return %")]
    public double AvgReturnPercent { get; init; }
}
