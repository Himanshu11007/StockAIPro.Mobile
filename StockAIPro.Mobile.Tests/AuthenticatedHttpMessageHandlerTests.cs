using System.Net;
using StockAIPro.Mobile.Services.Authentication;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class AuthenticatedHttpMessageHandlerTests
{
    [Fact]
    public async Task Attaches_bearer_token_from_the_token_store()
    {
        var tokenStore = new InMemoryTokenStore("stored-access-token", "stored-refresh-token");
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = BuildClient(tokenStore, new FakeAuthApiClient(), inner);

        await client.GetAsync("/whoami");

        var request = Assert.Single(inner.Requests);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("stored-access-token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task On_401_refreshes_once_and_retries_the_original_request_once()
    {
        var tokenStore = new InMemoryTokenStore("old-access", "refresh-token");
        var api = new FakeAuthApiClient { RefreshResult = () => Canned.Tokens("new-access", "new-refresh") };
        var inner = new TokenAwareHandler("new-access");
        using var client = BuildClient(tokenStore, api, inner);

        var response = await client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, api.RefreshCallCount);
        Assert.Equal("refresh-token", api.RefreshTokensUsed[0]);
        Assert.Equal(2, inner.Requests.Count); // original (401) + retry (200)
        Assert.Equal("new-access", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("new-refresh", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task Does_not_retry_more_than_once_even_if_the_retried_request_also_401s()
    {
        var tokenStore = new InMemoryTokenStore("old-access", "refresh-token");
        var api = new FakeAuthApiClient { RefreshResult = () => Canned.Tokens("new-access", "new-refresh") };
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)); // always 401
        using var client = BuildClient(tokenStore, api, inner);

        var response = await client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, api.RefreshCallCount); // refreshed once, did not loop
        Assert.Equal(2, inner.Requests.Count);  // original + exactly one retry
        // even the "new" token was rejected - credentials cleared so the
        // app stops presenting itself as authenticated.
        Assert.Null(await tokenStore.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Refresh_failure_clears_credentials_and_does_not_retry()
    {
        var tokenStore = new InMemoryTokenStore("old-access", "refresh-token");
        var api = new FakeAuthApiClient
        {
            RefreshResult = () => throw new Services.Api.ApiException(
                Services.Api.ApiErrorKind.Unauthorized, "Invalid or expired refresh token", 401),
        };
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = BuildClient(tokenStore, api, inner);

        var response = await client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, api.RefreshCallCount);
        Assert.Single(inner.Requests); // never retried - no point after refresh itself failed
        Assert.Null(await tokenStore.GetAccessTokenAsync());
        Assert.Null(await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task Refresh_5xx_preserves_credentials_and_surfaces_server_error()
    {
        var tokenStore = new InMemoryTokenStore("old-access", "refresh-token");
        var api = new FakeAuthApiClient
        {
            RefreshResult = () => throw new Services.Api.ApiException(
                Services.Api.ApiErrorKind.ServerError, "Internal server error", 500),
        };
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = BuildClient(tokenStore, api, inner);

        var ex = await Assert.ThrowsAsync<Services.Api.ApiException>(() => client.GetAsync("/whoami"));

        Assert.Equal(Services.Api.ApiErrorKind.ServerError, ex.Kind);
        Assert.Equal(1, api.RefreshCallCount);
        Assert.Equal("old-access", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-token", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task Refresh_network_failure_preserves_credentials()
    {
        var tokenStore = new InMemoryTokenStore("old-access", "refresh-token");
        var api = new FakeAuthApiClient
        {
            RefreshResult = () => throw Services.Api.ApiException.NetworkUnavailable(new HttpRequestException("boom")),
        };
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = BuildClient(tokenStore, api, inner);

        var ex = await Assert.ThrowsAsync<Services.Api.ApiException>(() => client.GetAsync("/whoami"));

        Assert.Equal(Services.Api.ApiErrorKind.NetworkUnavailable, ex.Kind);
        Assert.Equal(1, api.RefreshCallCount);
        Assert.Equal("old-access", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-token", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task Logout_between_401_and_acquiring_the_refresh_lock_does_not_use_the_stale_refresh_token()
    {
        var tokenStore = new InMemoryTokenStore("old-access", "refresh-token");
        tokenStore.OnGetRefreshToken = callIndex =>
        {
            // callIndex 1 = the read that happens right after the 401,
            // before the lock is acquired. callIndex 2 = the re-read taken
            // after acquiring RefreshLock. Simulate a logout landing in
            // between, exactly the ordering the race depends on.
            if (callIndex == 2)
                tokenStore.ClearAsync().GetAwaiter().GetResult();
        };
        var api = new FakeAuthApiClient { RefreshResult = () => Canned.Tokens("new-access", "new-refresh") };
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = BuildClient(tokenStore, api, inner);

        var response = await client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, api.RefreshCallCount); // never called refresh with the stale token
        Assert.Null(await tokenStore.GetAccessTokenAsync());
        Assert.Null(await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task Logout_while_refresh_is_in_flight_does_not_resurrect_the_session()
    {
        var tokenStore = new InMemoryTokenStore("old-access", "refresh-token");
        var api = new FakeAuthApiClient
        {
            RefreshResult = () =>
            {
                // Simulate the user logging out while this refresh call
                // (already sent to the server with the old, still-valid
                // refresh token) is still in flight.
                tokenStore.ClearAsync().GetAwaiter().GetResult();
                return Canned.Tokens("new-access", "new-refresh");
            },
        };
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = BuildClient(tokenStore, api, inner);

        var response = await client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        // The refresh technically succeeded server-side, but the user
        // logged out locally before it completed - the new tokens must
        // never be persisted, or logout would effectively be undone.
        Assert.Null(await tokenStore.GetAccessTokenAsync());
        Assert.Null(await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task Account_switch_between_401_and_acquiring_the_refresh_lock_never_replays_the_old_request()
    {
        // Reproduces the exact race described in the corrective-hardening
        // task: User A's request 401s and captures User A's session
        // identity; before this request can acquire the refresh lock, User
        // A logs out AND User B logs in (a full account switch, not just a
        // logout) - the old request must never be retried using User B's
        // credentials, and User B's session must be left completely
        // untouched.
        var tokenStore = new InMemoryTokenStore("A-access", "A-refresh");
        tokenStore.OnGetSessionId = callIndex =>
        {
            // callIndex 1 = the read taken when the request is first sent
            // (before the 401). callIndex 2 = the re-read taken after
            // acquiring RefreshLock. Simulate the account switch landing
            // exactly in between, which is what the race depends on.
            if (callIndex == 2)
            {
                tokenStore.ClearAsync().GetAwaiter().GetResult();
                tokenStore.SaveTokensAsync("B-access", "B-refresh", isNewSession: true).GetAwaiter().GetResult();
            }
        };
        var api = new FakeAuthApiClient { RefreshResult = () => Canned.Tokens("A-access-2", "A-refresh-2") };
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = BuildClient(tokenStore, api, inner);

        var response = await client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, api.RefreshCallCount); // never attempted to refresh User A's stale session
        Assert.Single(inner.Requests); // never retried against User B's session
        // User B's freshly-established session must be completely untouched
        // by User A's stale request - neither refreshed nor cleared.
        Assert.Equal("B-access", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("B-refresh", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task Account_switch_while_refresh_is_in_flight_does_not_overwrite_the_new_session()
    {
        // Same race, but the account switch happens WHILE the old request's
        // own refresh call is in flight (rather than before it starts) -
        // covers the second session-identity re-check, taken after the
        // network round-trip completes.
        var tokenStore = new InMemoryTokenStore("A-access", "A-refresh");
        var api = new FakeAuthApiClient
        {
            RefreshResult = () =>
            {
                // The refresh technically succeeds server-side for User A,
                // but User B has since logged in locally while the call was
                // in flight.
                tokenStore.ClearAsync().GetAwaiter().GetResult();
                tokenStore.SaveTokensAsync("B-access", "B-refresh", isNewSession: true).GetAwaiter().GetResult();
                return Canned.Tokens("A-access-2", "A-refresh-2");
            },
        };
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = BuildClient(tokenStore, api, inner);

        var response = await client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, api.RefreshCallCount);
        Assert.Single(inner.Requests); // never retried against User B's session
        // User A's refreshed tokens must never be saved over User B's
        // now-active session.
        Assert.Equal("B-access", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("B-refresh", await tokenStore.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task No_refresh_token_stored_surfaces_401_without_attempting_refresh()
    {
        var tokenStore = new InMemoryTokenStore("old-access", refreshToken: null!);
        var api = new FakeAuthApiClient();
        var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = BuildClient(tokenStore, api, inner);

        var response = await client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, api.RefreshCallCount);
    }

    [Fact]
    public async Task Concurrent_401s_trigger_exactly_one_refresh_call()
    {
        var tokenStore = new InMemoryTokenStore("old-access", "refresh-token");
        var api = new FakeAuthApiClient { RefreshResult = () => Canned.Tokens("new-access", "new-refresh") };
        var inner = new TokenAwareHandler("new-access");
        using var client = BuildClient(tokenStore, api, inner);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.GetAsync("/whoami")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(1, api.RefreshCallCount); // single-flight: not 8 refresh calls
    }

    private static HttpClient BuildClient(InMemoryTokenStore tokenStore, FakeAuthApiClient api, HttpMessageHandler inner)
    {
        var handler = new AuthenticatedHttpMessageHandler(tokenStore, api) { InnerHandler = inner };
        return new HttpClient(handler) { BaseAddress = new Uri("http://backend.test") };
    }

    /// <summary>Returns a fixed response for every request, recording each one.</summary>
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    /// <summary>Simulates a real backend: 200 only if the request carries
    /// the given (post-refresh) bearer token, 401 otherwise. Behavior
    /// depends only on the token presented, not on call order - makes the
    /// concurrency test deterministic regardless of scheduling.</summary>
    private sealed class TokenAwareHandler(string validAccessToken) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            lock (Requests) Requests.Add(request);
            var token = request.Headers.Authorization?.Parameter;
            var status = token == validAccessToken ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
