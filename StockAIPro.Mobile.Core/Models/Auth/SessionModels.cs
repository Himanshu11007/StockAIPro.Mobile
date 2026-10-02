using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>Matches api/schemas_auth.py:SessionResponse.</summary>
public sealed class SessionResponse
{
    [JsonPropertyName("session_id")]
    public int SessionId { get; init; }

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>Matches api/schemas_auth.py:RevokeSessionRequest.</summary>
public sealed class RevokeSessionRequest
{
    [JsonPropertyName("session_id")]
    public int SessionId { get; init; }
}

/// <summary>Matches api/schemas_auth.py:RevokeAllSessionsRequest.</summary>
public sealed class RevokeAllSessionsRequest
{
    [JsonPropertyName("except_current")]
    public bool ExceptCurrent { get; init; }

    [JsonPropertyName("current_device_id")]
    public string? CurrentDeviceId { get; init; }
}

/// <summary>Matches api/schemas_auth.py:RevokeAllSessionsResponse.</summary>
public sealed class RevokeAllSessionsResponse
{
    [JsonPropertyName("revoked_count")]
    public int RevokedCount { get; init; }
}

/// <summary>Matches api/schemas_auth.py:SetPinEnabledRequest. Carries only
/// the purely-informational pin_enabled flag - never the PIN itself, which
/// never leaves the device (see Services/Authentication/PinService.cs).</summary>
public sealed class SetPinEnabledRequest
{
    [JsonPropertyName("device_id")]
    public required string DeviceId { get; init; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }
}
