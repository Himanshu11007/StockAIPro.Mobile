using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>Matches api/schemas_auth.py:GoogleAuthRequest.</summary>
public sealed class GoogleAuthRequest
{
    [JsonPropertyName("id_token")]
    public required string IdToken { get; init; }

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; init; }
}
