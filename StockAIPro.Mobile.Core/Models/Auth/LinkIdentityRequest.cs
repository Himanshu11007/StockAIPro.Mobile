using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>Matches api/schemas_auth.py:LinkGoogleRequest.</summary>
public sealed class LinkGoogleRequest
{
    [JsonPropertyName("id_token")]
    public required string IdToken { get; init; }
}

/// <summary>Matches api/schemas_auth.py:LinkAppleRequest.</summary>
public sealed class LinkAppleRequest
{
    [JsonPropertyName("identity_token")]
    public required string IdentityToken { get; init; }
}

/// <summary>Matches api/schemas_auth.py:LinkedIdentityResponse.</summary>
public sealed class LinkedIdentityResponse
{
    [JsonPropertyName("provider")]
    public required string Provider { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}
