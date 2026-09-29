namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Persists the access/refresh token pair. The only implementation shipped
/// (SecureTokenStore) uses MAUI's platform secure storage (Android
/// Keystore-backed, etc.) - never plain files, preferences, or app state.
/// Abstracted as an interface so Services/Authentication's actual logic
/// (AuthService, AuthenticatedHttpMessageHandler) can be unit tested with
/// an in-memory fake instead of a real platform-backed store.
/// </summary>
public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Opaque identifier for the currently active session. A fresh value is
    /// generated every time <see cref="SaveTokensAsync"/> is called with
    /// <c>isNewSession: true</c> (login/register), stays unchanged across
    /// calls with <c>isNewSession: false</c> (a token-refresh rotating the
    /// pair within the same session), and becomes null after
    /// <see cref="ClearAsync"/> (logout).
    ///
    /// Exists so AuthenticatedHttpMessageHandler can tell "this session was
    /// refreshed by another request" apart from "the user logged out and a
    /// different (or the same, re-logged-in) account is now active" - a
    /// changed access token alone cannot distinguish those two cases, and
    /// conflating them lets a request captured under one account be replayed
    /// against a completely different session that has since logged in.
    /// </summary>
    Task<string?> GetSessionIdAsync();

    /// <summary>Persists a new access/refresh token pair. Pass
    /// <paramref name="isNewSession"/> = true only when this call
    /// establishes a brand new authenticated session (login/register);
    /// leave it false when rotating tokens for the session that is already
    /// active (a token refresh), which must not change the session
    /// identity <see cref="GetSessionIdAsync"/> reports.</summary>
    Task SaveTokensAsync(string accessToken, string refreshToken, bool isNewSession = false);

    Task ClearAsync();
}
