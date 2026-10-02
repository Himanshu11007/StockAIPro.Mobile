using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>Matches api/schemas_auth.py:OtpRequestRequest.</summary>
public sealed class OtpRequestRequest
{
    [JsonPropertyName("destination")]
    public required string Destination { get; init; }
}

/// <summary>Matches api/schemas_auth.py:OtpVerifyRequest.</summary>
public sealed class OtpVerifyRequest
{
    [JsonPropertyName("destination")]
    public required string Destination { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; init; }
}
