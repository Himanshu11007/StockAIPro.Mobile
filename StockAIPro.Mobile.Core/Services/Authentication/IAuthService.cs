using StockAIPro.Mobile.Models.Auth;

namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Application-level authentication state + orchestration. Razor components
/// read IsAuthenticated/CurrentUser and subscribe to AuthStateChanged rather
/// than talking to IAuthApiClient directly.
///
/// Client-side roles (CurrentUser?.Roles) are for UI presentation only
/// (e.g. showing/hiding a menu entry) - they are never used to grant access
/// to anything the backend itself would reject. The backend remains the
/// sole authority for what a request is actually allowed to do.
/// </summary>
public interface IAuthService
{
    bool IsAuthenticated { get; }
    UserProfileResponse? CurrentUser { get; }

    event Action? AuthStateChanged;

    Task RegisterAsync(string email, string password, CancellationToken ct = default);
    Task LoginAsync(string email, string password, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<UserProfileResponse> RefreshCurrentUserAsync(CancellationToken ct = default);

    /// <summary>Called once at app startup. Returns true if a valid session
    /// was restored from stored credentials, false otherwise (no stored
    /// credentials, invalid/expired refresh token, or backend unreachable -
    /// callers should not treat "false" as necessarily an error to show).</summary>
    Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);

    // ── Phase 8: Google / Apple / OTP sign-in ───────────────────────────────
    // Each establishes a brand new session exactly like LoginAsync/
    // RegisterAsync (isNewSession: true) - see AuthService.EstablishSessionAsync.

    Task LoginWithGoogleAsync(string idToken, CancellationToken ct = default);
    Task LoginWithAppleAsync(string identityToken, CancellationToken ct = default);
    Task RequestOtpAsync(string destination, CancellationToken ct = default);
    Task VerifyOtpAsync(string destination, string code, CancellationToken ct = default);

    // ── Forgot / reset password ─────────────────────────────────────────────
    // Neither signs anyone in: after a reset the user signs in with the new
    // password (every previous session, including one on this device, was
    // revoked by the backend).

    /// <summary>Returns the backend's generic confirmation message - identical
    /// whether or not the email has an account.</summary>
    Task<string> RequestPasswordResetAsync(string email, CancellationToken ct = default);

    Task<string> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);

    // ── Phase 8: account linking ─────────────────────────────────────────────

    Task<LinkedIdentityResponse> LinkGoogleAsync(string idToken, CancellationToken ct = default);
    Task<LinkedIdentityResponse> LinkAppleAsync(string identityToken, CancellationToken ct = default);
    Task<List<LinkedIdentityResponse>> GetLinkedIdentitiesAsync(CancellationToken ct = default);
    Task UnlinkIdentityAsync(string provider, CancellationToken ct = default);

    // ── Phase 8: device / session management ────────────────────────────────

    Task<string> GetDeviceIdAsync();
    Task<List<SessionResponse>> GetSessionsAsync(CancellationToken ct = default);
    Task RevokeSessionAsync(int sessionId, CancellationToken ct = default);

    /// <summary>Sign out all devices (exceptCurrent: false) or every OTHER
    /// device (exceptCurrent: true). When exceptCurrent is false, this
    /// device's own session was just revoked too - local credentials are
    /// cleared and IsAuthenticated flips to false immediately, same as
    /// LogoutAsync. Returns the number of sessions revoked.</summary>
    Task<int> SignOutAllDevicesAsync(bool exceptCurrent, CancellationToken ct = default);

    /// <summary>Tells the backend whether this device has a local PIN
    /// configured - purely informational, never sends the PIN itself.</summary>
    Task SetPinEnabledAsync(bool enabled, CancellationToken ct = default);
}
