using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>Matches api/schemas_auth.py:LogoutRequest (refresh_token).</summary>
public sealed class LogoutRequest
{
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; init; }
}
