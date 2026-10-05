using System.Text.Json.Serialization;

namespace StockAIPro.Mobile.Models.Auth;

/// <summary>Matches api/schemas_auth.py:ForgotPasswordRequest.</summary>
public sealed class ForgotPasswordRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }
}

/// <summary>Matches api/schemas_auth.py:ResetPasswordRequest.</summary>
public sealed class ResetPasswordRequest
{
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    [JsonPropertyName("new_password")]
    public required string NewPassword { get; init; }
}

/// <summary>Matches api/schemas_auth.py:MessageResponse.</summary>
public sealed class MessageResponse
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = "";
}
