using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>
/// Matches api/schemas_auth.py:UserProfileResponse exactly, as returned by
/// GET /auth/me. This is the authoritative current-user representation -
/// never construct one locally from the login email.
/// </summary>
public sealed class UserProfileResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; init; } = [];

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}
