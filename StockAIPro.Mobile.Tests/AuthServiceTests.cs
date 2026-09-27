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
        var service = new AuthService(api, tokenStore);
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
        var service = new AuthService(api, new InMemoryTokenStore());

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
        var service = new AuthService(failingLogoutApi, tokenStore);

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
        var service = new AuthService(new FakeAuthApiClient(), new InMemoryTokenStore());

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task TryRestoreSessionAsync_with_valid_stored_credentials_returns_true()
    {
        var tokenStore = new InMemoryTokenStore("access-1", "refresh-1");
        var api = new FakeAuthApiClient { CurrentUserResult = () => Canned.Profile("restored@example.com") };
        var service = new AuthService(api, tokenStore);

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
        var service = new AuthService(api, tokenStore);

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
        var service = new AuthService(api, tokenStore);

        var restored = await service.TryRestoreSessionAsync();

        Assert.False(restored);
        Assert.False(service.IsAuthenticated);
        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-1", await tokenStore.GetRefreshTokenAsync());
    }

    /// <summary>Wraps a FakeAuthApiClient so LogoutAsync always throws,
    /// while delegating everything else - simulates a server-side logout
    /// failure without needing a whole new fake implementation.</summary>
    private sealed class ThrowingLogoutAuthApiClient(FakeAuthApiClient inner) : Services.Api.IAuthApiClient
    {
        public Task<Models.Auth.TokenResponse> RegisterAsync(string email, string password, CancellationToken ct = default) =>
            inner.RegisterAsync(email, password, ct);

        public Task<Models.Auth.TokenResponse> LoginAsync(string email, string password, CancellationToken ct = default) =>
            inner.LoginAsync(email, password, ct);

        public Task<Models.Auth.TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
            inner.RefreshAsync(refreshToken, ct);

        public Task LogoutAsync(string refreshToken, CancellationToken ct = default) =>
            throw new ApiException(ApiErrorKind.NetworkUnavailable, "down");

        public Task<Models.Auth.UserProfileResponse> GetCurrentUserAsync(CancellationToken ct = default) =>
            inner.GetCurrentUserAsync(ct);
    }
}
