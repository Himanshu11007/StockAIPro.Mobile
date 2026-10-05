using StockAIPro.Mobile.Models.Auth;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// Raw calls to /api/v1/auth/* - exactly the six endpoints that exist on
/// the backend today (api/routes/auth.py). No token-refresh/retry logic
/// lives here; that belongs to Services/Authentication (which uses this
/// client, notably for the refresh call itself - keeping this client
/// "dumb" avoids a circular dependency between the two).
/// </summary>
public interface IAuthApiClient
{
    Task<TokenResponse> RegisterAsync(
        string email, string password, string? deviceId = null, string? deviceName = null, CancellationToken ct = default);

    /// <summary>OAuth2 password flow: backend expects form fields
    /// username/password(/device_id/device_name), not JSON - see
    /// api/routes/auth.py:login() / FastAPI's OAuth2PasswordRequestForm.</summary>
    Task<TokenResponse> LoginAsync(
        string email, string password, string? deviceId = null, string? deviceName = null, CancellationToken ct = default);

    Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Backend returns 204 No Content on success - nothing to
    /// return. Caller clears local credentials regardless of outcome.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// GET /auth/me. Unlike the other methods here, this one is routed
    /// through the authenticated HTTP pipeline (Services/Authentication) so
    /// the Authorization header is attached automatically and a 401 can
    /// trigger the same refresh-and-retry-once flow as any other
    /// authenticated call - this IS the "real authenticated request" the
    /// Phase 7A objective asks for, not a separate demo call.
    /// </summary>
    Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken ct = default);

    // ── Phase 8: Google / Apple / OTP sign-in ───────────────────────────────
    // Unauthenticated (RawClientName), exactly like Login/Register above -
    // these establish a brand new session, so there is no access token to
    // attach yet.

    Task<TokenResponse> LoginWithGoogleAsync(string idToken, string? deviceId, string? deviceName, CancellationToken ct = default);

    Task<TokenResponse> LoginWithAppleAsync(string identityToken, string? deviceId, string? deviceName, CancellationToken ct = default);

    /// <summary>Backend returns 204 regardless of whether the destination
    /// has an account (can't be used to enumerate registered users) -
    /// nothing to return here either.</summary>
    Task RequestOtpAsync(string destination, CancellationToken ct = default);

    Task<TokenResponse> VerifyOtpAsync(string destination, string code, string? deviceId, string? deviceName, CancellationToken ct = default);

    // ── Forgot / reset password ────────────────────────────────────────────
    // Unauthenticated (RawClientName). Both return the backend's message.

    /// <summary>POST /auth/forgot-password. The backend answers 202 with the
    /// same generic message whether or not the email has an account, so the
    /// result never tells the caller whether an account exists. Only an
    /// invalid email (422) or too many requests (429) throw.</summary>
    Task<string> ForgotPasswordAsync(string email, CancellationToken ct = default);

    /// <summary>POST /auth/reset-password with the token from the emailed
    /// link. 400 = invalid/expired/used link, 422 = password rejected by the
    /// password policy (the link stays valid), 429 = too many attempts. On
    /// success every existing session of the account has been revoked.</summary>
    Task<string> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);

    // ── Phase 8: account linking, device/session management ────────────────
    // Authenticated (AuthenticatedClientName) - all require an existing
    // session, routed through AuthenticatedHttpMessageHandler like GetCurrentUserAsync.

    Task<LinkedIdentityResponse> LinkGoogleAsync(string idToken, CancellationToken ct = default);

    Task<LinkedIdentityResponse> LinkAppleAsync(string identityToken, CancellationToken ct = default);

    Task<List<LinkedIdentityResponse>> GetLinkedIdentitiesAsync(CancellationToken ct = default);

    Task UnlinkIdentityAsync(string provider, CancellationToken ct = default);

    Task<List<SessionResponse>> GetSessionsAsync(CancellationToken ct = default);

    Task RevokeSessionAsync(int sessionId, CancellationToken ct = default);

    Task<RevokeAllSessionsResponse> RevokeAllSessionsAsync(bool exceptCurrent, string? currentDeviceId, CancellationToken ct = default);

    /// <summary>Flips the purely-informational pin_enabled flag on this
    /// device's trusted-device record. Never sends the PIN itself - see
    /// Services/Authentication/PinService.cs.</summary>
    Task SetPinEnabledAsync(string deviceId, bool enabled, CancellationToken ct = default);
}
