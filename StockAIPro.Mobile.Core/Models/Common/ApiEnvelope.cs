using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Common;

/// <summary>
/// Every /api/v1 business endpoint (everything except /auth/*) wraps a
/// successful response as {"success": true, "data": ..., "message": "..."}
/// — see api/main.py's own "Response contract" docstring. Auth endpoints are
/// the one exception (they return the raw model directly), which is why
/// AuthApiClient does not use this type.
/// </summary>
public sealed class ApiEnvelope<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public required T Data { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = "";
}
