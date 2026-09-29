using System.Net;
using System.Net.Http.Headers;
using StockAIPro.Mobile.Models.Auth;
using StockAIPro.Mobile.Services.Api;

namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Attaches "Authorization: Bearer &lt;access_token&gt;" to every outgoing
/// request, and on a 401 response: refreshes the token pair once (via
/// POST /auth/refresh) and retries the original request exactly once with
/// the new access token. If refresh itself fails, clears stored
/// credentials and returns the 401 to the caller rather than looping.
///
/// Concurrent-refresh protection: a static SemaphoreSlim(1,1) ensures that
/// if several requests hit 401 around the same time, only one of them
/// actually calls /auth/refresh - the others wait, then re-check whether
/// the access token already changed (another request refreshed it while
/// they waited) before deciding whether to refresh again themselves.
///
/// Depends only on ITokenStore and IAuthApiClient (not on IAuthService),
/// deliberately - IAuthApiClient's raw refresh call is what this handler
/// needs, and depending on the higher-level IAuthService here would create
/// a circular DI dependency (IAuthService uses an HttpClient that has this
/// handler attached).
/// </summary>
public sealed class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static readonly HttpRequestOptionsKey<bool> RetriedKey = new("StockAIPro.RetriedAfterRefresh");

    private readonly ITokenStore _tokenStore;
    private readonly IAuthApiClient _authApiClient;

    public AuthenticatedHttpMessageHandler(ITokenStore tokenStore, IAuthApiClient authApiClient)
    {
        _tokenStore = tokenStore;
        _authApiClient = authApiClient;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessTokenUsed = await AttachAccessTokenAsync(request);
        // Captured at the same time as the access token, before the request
        // is even sent - see the session-identity check below for why.
        var sessionIdUsed = await _tokenStore.GetSessionIdAsync();

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Only ever retry once per original request, no matter how many
        // times it round-trips through here. The retry itself is sent via
        // base.SendAsync (below), not by recursing into this method, so in
        // this handler's own control flow a retried request never actually
        // reaches this check - it exists as a safety net in case a retry
        // is ever routed back through here (e.g. a future refactor).
        if (request.Options.TryGetValue(RetriedKey, out var alreadyRetried) && alreadyRetried)
        {
            await _tokenStore.ClearAsync();
            return response;
        }

        var refreshTokenAtFailure = await _tokenStore.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshTokenAtFailure))
            return response; // nothing to refresh with - surface the 401 as-is

        response.Dispose();

        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Session-identity guard: re-read which session is active NOW
            // that we hold the lock. If it differs from the session this
            // request was originally sent under, the account/session has
            // changed since this request failed - a logout, a logout
            // followed by a login (same or different account), all bump
            // the session id (see ITokenStore.GetSessionIdAsync). This
            // request must never be retried against whatever credentials
            // happen to be active now: without this check, the access-token
            // comparison just below would see a "changed" access token and
            // wrongly conclude "another request already refreshed MY
            // session," when what actually happened is a completely
            // different session is now logged in.
            var currentSessionId = await _tokenStore.GetSessionIdAsync();
            if (currentSessionId != sessionIdUsed)
                return UnauthorizedResponse(request, "Session changed - not retrying under a different session");

            // Another request may have already refreshed while we waited
            // for the lock - if the stored access token changed since we
            // sent our request (and, per the check above, it's still the
            // same session), use it directly instead of refreshing again.
            var currentAccessToken = await _tokenStore.GetAccessTokenAsync();
            string? newAccessToken = currentAccessToken != accessTokenUsed ? currentAccessToken : null;

            if (newAccessToken is null)
            {
                // Re-read the refresh token now that we actually hold the
                // lock. It may have been rotated by a request that
                // refreshed while we waited, or cleared entirely by a
                // concurrent logout. Refreshing with the token captured
                // before the failed request would risk reusing a token the
                // rest of the app no longer considers valid (e.g. resuming
                // a session the user just logged out of).
                var refreshTokenNow = await _tokenStore.GetRefreshTokenAsync();
                if (string.IsNullOrEmpty(refreshTokenNow) || refreshTokenNow != refreshTokenAtFailure)
                    return UnauthorizedResponse(request, "Session ended before refresh could run");

                TokenResponse tokens;
                try
                {
                    tokens = await _authApiClient.RefreshAsync(refreshTokenNow, cancellationToken);
                }
                catch (ApiException ex) when (ex.Kind == ApiErrorKind.Unauthorized)
                {
                    // Refresh token is genuinely invalid/expired/revoked -
                    // the user must sign in again. Clear credentials so the
                    // app stops presenting itself as authenticated. No
                    // point retrying the original request (it would just
                    // 401 again) - return a synthetic 401 directly.
                    await _tokenStore.ClearAsync();
                    return UnauthorizedResponse(request, "Session expired - refresh failed");
                }
                // Any other ApiException kind (NetworkUnavailable,
                // ServerError, Unknown, ...) is a transient transport/server
                // problem, not proof the session itself is invalid - let it
                // propagate to the caller and leave stored tokens untouched
                // so a subsequent request can retry once connectivity/the
                // server recovers.

                // The refresh call may have taken a while; if a logout (or
                // logout-then-login as a different/same account) happened
                // while it was in flight, discard the result rather than
                // resurrecting a session the user already ended, or - worse
                // - saving User A's refreshed tokens on top of User B's
                // now-active session.
                var refreshTokenAfterCall = await _tokenStore.GetRefreshTokenAsync();
                var sessionIdAfterCall = await _tokenStore.GetSessionIdAsync();
                if (refreshTokenAfterCall != refreshTokenNow || sessionIdAfterCall != sessionIdUsed)
                    return UnauthorizedResponse(request, "Session ended during refresh");

                await _tokenStore.SaveTokensAsync(tokens.AccessToken, tokens.RefreshToken);
                newAccessToken = tokens.AccessToken;
            }

            var retryRequest = await CloneRequestAsync(request, newAccessToken, retried: true);
            var retryResponse = await base.SendAsync(retryRequest, cancellationToken);

            if (retryResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Even the freshly (or already, if reused above)
                // refreshed token was rejected - clear credentials rather
                // than leave the app thinking it's still authenticated.
                // No further retry: the caller already sees this 401.
                await _tokenStore.ClearAsync();
            }

            return retryResponse;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static HttpResponseMessage UnauthorizedResponse(HttpRequestMessage request, string reason) =>
        new(HttpStatusCode.Unauthorized) { RequestMessage = request, ReasonPhrase = reason };

    private async Task<string?> AttachAccessTokenAsync(HttpRequestMessage request)
    {
        var accessToken = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return accessToken;
    }

    /// <summary>
    /// HttpRequestMessage instances can only be sent once - a retry needs a
    /// fresh instance with the same method/URI/headers/content and the new
    /// bearer token.
    /// </summary>
    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage original, string? accessToken, bool retried)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version,
        };

        if (original.Content is not null)
        {
            var buffer = await original.Content.ReadAsByteArrayAsync();
            var content = new ByteArrayContent(buffer);
            foreach (var header in original.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = content;
        }

        foreach (var header in original.Headers)
        {
            if (header.Key == "Authorization")
                continue; // replaced below
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (!string.IsNullOrEmpty(accessToken))
            clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        foreach (var (key, value) in original.Options)
            clone.Options.TryAdd(key, value);
        if (retried)
            clone.Options.Set(RetriedKey, true);

        return clone;
    }
}
