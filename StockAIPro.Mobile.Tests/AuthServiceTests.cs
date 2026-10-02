using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Authentication;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_stores_tokens_and_establishes_authenticated_state()
    {
        var tokenStore = new InMemoryTokenStore();
        var api = new FakeAuthApiClient
        {
            LoginResult = () => Canned.Tokens("access-1", "refresh-1"),
            CurrentUserResult = () => Canned.Profile("a@example.com"),
        };
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());
        var stateChangedCount = 0;
        service.AuthStateChanged += () => stateChangedCount++;

        await service.LoginAsync("a@example.com", "password1");

        Assert.True(service.IsAuthenticated);
        Assert.Equal("a@example.com", service.CurrentUser?.Email);
        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-1", await tokenStore.GetRefreshTokenAsync());
        Assert.True(stateChangedCount >= 1);
        // /auth/me was called as part of establishing the session - the
        // authoritative source for CurrentUser, not the login email alone.
        Assert.Equal(1, api.GetCurrentUserCallCount);
    }

    [Fact]
    public async Task RegisterAsync_also_establishes_authenticated_state()
    {
        var api = new FakeAuthApiClient { RegisterResult = () => Canned.Tokens("access-r", "refresh-r") };
        var service = new AuthService(api, new InMemoryTokenStore(), new InMemoryDeviceIdentityService());

        await service.RegisterAsync("new@example.com", "password1");

        Assert.True(service.IsAuthenticated);
        Assert.Equal(1, api.RegisterCallCount);
        Assert.Equal(("new@example.com", "password1"), api.LastRegisterArgs);
    }

    [Fact]
    public async Task LogoutAsync_clears_credentials_even_when_server_call_fails()
    {
        var tokenStore = new InMemoryTokenStore();
        // Same service instance throughout, so the True -> False transition
        // below is actually exercised (not just asserted against a fresh,
        // never-logged-in instance).
        var api = new FakeAuthApiClient();
        var failingLogoutApi = new ThrowingLogoutAuthApiClient(api);
        var service = new AuthService(failingLogoutApi, tokenStore, new InMemoryDeviceIdentityService());

        await service.LoginAsync("a@example.com", "password1"); // delegates to the inner fake, succeeds
        Assert.True(service.IsAuthenticated);

        // Server-side logout call fails (e.g. network drop) - the device
        // must still stop presenting as authenticated.
        await service.LogoutAsync();

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.CurrentUser);
        Assert.Null(await tokenStore.GetAccessTokenAsync());
        Assert.Null(await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task TryRestoreSessionAsync_with_no_stored_credentials_returns_false()
    {
        var service = new AuthService(new FakeAuthApiClient(), new InMemoryTokenStore(), new InMemoryDeviceIdentityService());

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task TryRestoreSessionAsync_with_valid_stored_credentials_returns_true()
    {
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient { CurrentUserResult = () => Canned.Profile("restored@example.com") };
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());

        var restored = await service.TryRestoreSessionAsync();

        Assert.True(restored);
        Assert.True(service.IsAuthenticated);
        Assert.Equal("restored@example.com", service.CurrentUser?.Email);
    }

    [Fact]
    public async Task TryRestoreSessionAsync_clears_credentials_when_refresh_token_is_invalid()
    {
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient
        {
            CurrentUserResult = () => throw new ApiException(ApiErrorKind.Unauthorized, "Invalid or expired refresh token", 401),
        };
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
        Assert.Null(await tokenStore.GetAccessTokenAsync());
        Assert.Null(await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task TryRestoreSessionAsync_on_network_failure_does_not_clear_credentials()
    {
        // Backend unreachable at startup - stored tokens might still be
        // valid, so they must survive a transient network failure.
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient
        {
            CurrentUserResult = () => throw ApiException.NetworkUnavailable(new HttpRequestException("down")),
        };
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-1", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task TryRestoreSessionAsync_on_server_error_does_not_clear_credentials()
    {
        // A 5xx from /auth/me proves nothing about whether the stored
        // refresh token is valid - only an explicit 401 does.
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient
        {
            CurrentUserResult = () => throw new ApiException(ApiErrorKind.ServerError, "Internal server error", 500),
        };
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-1", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task TryRestoreSessionAsync_on_forbidden_does_not_clear_credentials()
    {
        // 403 means the account/refresh token is valid but lacks some
        // permission - not that the session itself is invalid.
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient
        {
            CurrentUserResult = () => throw new ApiException(ApiErrorKind.Forbidden, "Forbidden", 403),
        };
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-1", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task TryRestoreSessionAsync_on_too_many_requests_does_not_clear_credentials()
    {
        // Rate-limited, not unauthenticated - retrying later should still
        // work with the same stored credentials.
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient
        {
            CurrentUserResult = () => throw new ApiException(ApiErrorKind.TooManyRequests, "Too many requests", 429),
        };
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-1", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task TryRestoreSessionAsync_on_unknown_api_error_does_not_clear_credentials()
    {
        // An unrecognized failure kind is exactly the case where guessing
        // wrong is most costly - default to preserving credentials.
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient
        {
            CurrentUserResult = () => throw new ApiException(ApiErrorKind.Unknown, "Unexpected error", 599),
        };
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-1", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task TryRestoreSessionAsync_propagates_caller_cancellation_without_clearing_credentials()
    {
        // A caller-initiated cancellation (e.g. the app is shutting down or
        // navigating away) is not a statement about the session's validity
        // at all - it must propagate to the caller unchanged, and must not
        // be swallowed or mistaken for a 401.
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient();
        var service = new AuthService(api, tokenStore, new InMemoryDeviceIdentityService());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.TryRestoreSessionAsync(cts.Token));

        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-1", await tokenStore.GetRefreshTokenAsync());
    }

    /// <summary>Wraps a FakeAuthApiClient so LogoutAsync always throws,
    /// while delegating everything else - simulates a server-side logout
    /// failure without needing a whole new fake implementation.</summary>
    private sealed class ThrowingLogoutAuthApiClient(FakeAuthApiClient inner) : Services.Api.IAuthApiClient
    {
        public Task<Models.Auth.TokenResponse> RegisterAsync(
            string email, string password, string? deviceId = null, string? deviceName = null, CancellationToken ct = default) =>
            inner.RegisterAsync(email, password, deviceId, deviceName, ct);

        public Task<Models.Auth.TokenResponse> LoginAsync(
            string email, string password, string? deviceId = null, string? deviceName = null, CancellationToken ct = default) =>
            inner.LoginAsync(email, password, deviceId, deviceName, ct);

        public Task<Models.Auth.TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
            inner.RefreshAsync(refreshToken, ct);

        public Task LogoutAsync(string refreshToken, CancellationToken ct = default) =>
            throw new ApiException(ApiErrorKind.NetworkUnavailable, "down");

        public Task<Models.Auth.UserProfileResponse> GetCurrentUserAsync(CancellationToken ct = default) =>
            inner.GetCurrentUserAsync(ct);

        public Task<Models.Auth.TokenResponse> LoginWithGoogleAsync(string idToken, string? deviceId, string? deviceName, CancellationToken ct = default) =>
            inner.LoginWithGoogleAsync(idToken, deviceId, deviceName, ct);

        public Task<Models.Auth.TokenResponse> LoginWithAppleAsync(string identityToken, string? deviceId, string? deviceName, CancellationToken ct = default) =>
            inner.LoginWithAppleAsync(identityToken, deviceId, deviceName, ct);

        public Task RequestOtpAsync(string destination, CancellationToken ct = default) =>
            inner.RequestOtpAsync(destination, ct);

        public Task<Models.Auth.TokenResponse> VerifyOtpAsync(string destination, string code, string? deviceId, string? deviceName, CancellationToken ct = default) =>
            inner.VerifyOtpAsync(destination, code, deviceId, deviceName, ct);

        public Task<Models.Auth.LinkedIdentityResponse> LinkGoogleAsync(string idToken, CancellationToken ct = default) =>
            inner.LinkGoogleAsync(idToken, ct);

        public Task<Models.Auth.LinkedIdentityResponse> LinkAppleAsync(string identityToken, CancellationToken ct = default) =>
            inner.LinkAppleAsync(identityToken, ct);

        public Task<List<Models.Auth.LinkedIdentityResponse>> GetLinkedIdentitiesAsync(CancellationToken ct = default) =>
            inner.GetLinkedIdentitiesAsync(ct);

        public Task UnlinkIdentityAsync(string provider, CancellationToken ct = default) =>
            inner.UnlinkIdentityAsync(provider, ct);

        public Task<List<Models.Auth.SessionResponse>> GetSessionsAsync(CancellationToken ct = default) =>
            inner.GetSessionsAsync(ct);

        public Task RevokeSessionAsync(int sessionId, CancellationToken ct = default) =>
            inner.RevokeSessionAsync(sessionId, ct);

        public Task<Models.Auth.RevokeAllSessionsResponse> RevokeAllSessionsAsync(bool exceptCurrent, string? currentDeviceId, CancellationToken ct = default) =>
            inner.RevokeAllSessionsAsync(exceptCurrent, currentDeviceId, ct);

        public Task SetPinEnabledAsync(string deviceId, bool enabled, CancellationToken ct = default) =>
            inner.SetPinEnabledAsync(deviceId, enabled, ct);
    }
}
