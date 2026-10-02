using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>Matches api/schemas_auth.py:AppleAuthRequest.</summary>
public sealed class AppleAuthRequest
{
    [JsonPropertyName("identity_token")]
    public required string IdentityToken { get; init; }

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; init; }
}
