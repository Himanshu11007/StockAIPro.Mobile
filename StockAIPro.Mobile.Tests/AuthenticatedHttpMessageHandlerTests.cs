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
