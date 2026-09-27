using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>Matches api/schemas_auth.py:RegisterRequest (email, password).</summary>
public sealed class RegisterRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("password")]
    public required string Password { get; init; }
}
