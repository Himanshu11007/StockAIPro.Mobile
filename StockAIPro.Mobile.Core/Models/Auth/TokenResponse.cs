using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>
/// Matches api/schemas_auth.py:TokenResponse exactly, as returned by
/// POST /auth/register, /auth/login, and /auth/refresh. Verified against
/// the running backend (no expires_in or user field exists on this
/// response - do not add one).
/// </summary>
public sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "bearer";
}
