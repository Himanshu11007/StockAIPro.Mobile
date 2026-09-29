using StockAIPro.Mobile.Models.Auth;
using StockAIPro.Mobile.Services.Api;

namespace StockAIPro.Mobile.Services.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ITokenStore _tokenStore;

    public bool IsAuthenticated { get; private set; }
    public UserProfileResponse? CurrentUser { get; private set; }

    public event Action? AuthStateChanged;

    public AuthService(IAuthApiClient authApiClient, ITokenStore tokenStore)
    {
        _authApiClient = authApiClient;
        _tokenStore = tokenStore;
    }

    public async Task RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        // The backend issues a real token pair on successful registration
        // (see api/routes/auth.py:register -> TokenResponse, identical
        // shape to login) - establishing a session here reflects the
        // backend's actual behavior, it is not an assumption made on top of it.
        var tokens = await _authApiClient.RegisterAsync(email, password, ct);
        await EstablishSessionAsync(tokens, ct);
    }

    public async Task LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var tokens = await _authApiClient.LoginAsync(email, password, ct);
        await EstablishSessionAsync(tokens, ct);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var refreshToken = await _tokenStore.GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try
            {
                await _authApiClient.LogoutAsync(refreshToken, ct);
            }
            catch (ApiException)
            {
                // Best-effort server-side revocation. The device must stop
                // treating the user as authenticated either way, so clear
                // local credentials regardless of whether this succeeded.
            }
        }

        await _tokenStore.ClearAsync();
        SetUnauthenticated();
    }

    public async Task<UserProfileResponse> RefreshCurrentUserAsync(CancellationToken ct = default)
    {
        var profile = await _authApiClient.GetCurrentUserAsync(ct);
        CurrentUser = profile;
        IsAuthenticated = true;
        AuthStateChanged?.Invoke();
        return profile;
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
    {
        var accessToken = await _tokenStore.GetAccessTokenAsync();
        var refreshToken = await _tokenStore.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            SetUnauthenticated();
            return false;
        }

        try
        {
            // Goes through the authenticated pipeline - if the stored
            // access token has expired, AuthenticatedHttpMessageHandler
            // transparently refreshes it using the stored refresh token.
            await RefreshCurrentUserAsync(ct);
            return true;
        }
        catch (ApiException ex) when (ex.Kind == ApiErrorKind.Unauthorized)
        {
            // The ONLY response that actually proves the stored session is
            // invalid - refresh token expired/revoked, or the handler
            // already tried refreshing and that failed too.
            await _tokenStore.ClearAsync();
            SetUnauthenticated();
            return false;
        }
        catch (ApiException)
        {
            // Any other ApiException kind (NetworkUnavailable, ServerError,
            // Forbidden, TooManyRequests, Unknown, ...) does NOT prove the
            // session itself is invalid - backend unreachable, a transient
            // 5xx, rate limiting, or an ambiguous/unexpected response are
            // all reasons to try again later, not to silently sign the user
            // out. Leave stored credentials untouched. Don't block app
            // startup either; leave state unauthenticated-for-now and let
            // the user (or the next authenticated request) retry once
            // connectivity/the backend recovers.
            SetUnauthenticated();
            return false;
        }
        // A plain OperationCanceledException/TaskCanceledException (caller
        // cancellation - see AuthApiClient/BusinessApiSend, which never
        // wrap caller-initiated cancellation as an ApiException) is
        // deliberately NOT caught here - it propagates to the caller
        // unchanged rather than being reinterpreted as a failed restore.
    }

    private async Task EstablishSessionAsync(TokenResponse tokens, CancellationToken ct)
    {
        // isNewSession: true - login/register always starts a brand new
        // session identity, even if it's the same account re-logging in.
        // AuthenticatedHttpMessageHandler relies on this to tell a token
        // refresh (same session, tokens rotated) apart from an account
        // switch (different session entirely) - see its own comments.
        await _tokenStore.SaveTokensAsync(tokens.AccessToken, tokens.RefreshToken, isNewSession: true);
        await RefreshCurrentUserAsync(ct);
    }

    private void SetUnauthenticated()
    {
        IsAuthenticated = false;
        CurrentUser = null;
        AuthStateChanged?.Invoke();
    }
}
