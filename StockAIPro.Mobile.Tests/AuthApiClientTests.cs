using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Models.Auth;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class AuthApiClientTests
{
    // ── Request mapping ─────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_posts_form_urlencoded_username_and_password()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(Canned.Tokens()));
        var factory = new FakeHttpClientFactory(handler);
        var client = new AuthApiClient(factory);

        await client.LoginAsync("user@example.com", "s3cret!!");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"http://backend.test{ApiConfiguration.ApiPrefix}/auth/login", request.RequestUri!.ToString());
        Assert.Equal("application/x-www-form-urlencoded", request.Content!.Headers.ContentType!.MediaType);

        var body = handler.RequestBodies[0]!;
        Assert.Contains("username=user%40example.com", body);
        Assert.Contains("password=s3cret", body);
    }

    [Fact]
    public async Task RegisterAsync_posts_json_email_and_password()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(Canned.Tokens()));
        var factory = new FakeHttpClientFactory(handler);
        var client = new AuthApiClient(factory);

        await client.RegisterAsync("new@example.com", "s3cret!!");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"http://backend.test{ApiConfiguration.ApiPrefix}/auth/register", request.RequestUri!.ToString());
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);

        var body = handler.RequestBodies[0]!;
        Assert.Contains("\"email\":\"new@example.com\"", body);
        Assert.Contains("\"password\":\"s3cret!!\"", body);
    }

    [Fact]
    public async Task RefreshAsync_posts_refresh_token_and_returns_new_pair()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(Canned.Tokens("new-access", "new-refresh")));
        var factory = new FakeHttpClientFactory(handler);
        var client = new AuthApiClient(factory);

        var result = await client.RefreshAsync("old-refresh-token");

        Assert.Equal("new-access", result.AccessToken);
        Assert.Equal("new-refresh", result.RefreshToken);
        Assert.Contains("\"refresh_token\":\"old-refresh-token\"", handler.RequestBodies[0]);
    }

    // ── /auth/me goes through the authenticated (not raw) named client ──

    [Fact]
    public async Task GetCurrentUserAsync_uses_the_authenticated_client_not_the_raw_one()
    {
        var rawHandler = new FakeHttpMessageHandler();
        var authedHandler = new FakeHttpMessageHandler();
        authedHandler.Enqueue(HttpStatusCode.OK, JsonContent.Create(Canned.Profile("me@example.com")));

        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.RawClientName, rawHandler);
        factory.Configure(ApiConfiguration.AuthenticatedClientName, authedHandler);
        var client = new AuthApiClient(factory);

        var profile = await client.GetCurrentUserAsync();

        Assert.Equal("me@example.com", profile.Email);
        Assert.Empty(rawHandler.Requests);
        Assert.Single(authedHandler.Requests);
    }

    // ── Error mapping ────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_401_throws_Unauthorized_ApiException_with_backend_message()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonContent.Create(new { detail = "Invalid email or password" }),
        });
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.LoginAsync("a@b.com", "wrong"));

        Assert.Equal(ApiErrorKind.Unauthorized, ex.Kind);
        Assert.Equal("Invalid email or password", ex.Message);
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task RegisterAsync_409_throws_Conflict_ApiException()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(new { detail = "A user with email 'a@b.com' already exists" }),
        });
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.RegisterAsync("a@b.com", "password1"));

        Assert.Equal(ApiErrorKind.Conflict, ex.Kind);
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_422_validation_error_array_is_joined_into_one_message()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(new
            {
                detail = new[]
                {
                    new { loc = new[] { "body", "email" }, msg = "value is not a valid email address", type = "value_error" },
                    new { loc = new[] { "body", "password" }, msg = "String should have at least 8 characters", type = "string_too_short" },
                },
            }),
        });
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.RegisterAsync("bad", "x"));

        Assert.Equal(ApiErrorKind.ValidationFailed, ex.Kind);
        Assert.Contains("valid email address", ex.Message);
        Assert.Contains("at least 8 characters", ex.Message);
    }

    [Fact]
    public async Task Failed_refresh_error_message_never_contains_the_refresh_token_value()
    {
        const string secretRefreshToken = "super-secret-refresh-token-value";
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonContent.Create(new { detail = "Invalid or expired refresh token" }),
        });
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.RefreshAsync(secretRefreshToken));

        // The request body legitimately contains the token (it has to, to
        // reach the backend) - but nothing derived from the response/error
        // path (the exception the rest of the app sees and might log or
        // display) may ever echo it back.
        Assert.DoesNotContain(secretRefreshToken, ex.Message);
        Assert.DoesNotContain(secretRefreshToken, ex.ToString());
    }

    [Fact]
    public async Task Network_failure_throws_NetworkUnavailable_not_a_raw_exception()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueNetworkFailure();
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.LoginAsync("a@b.com", "password1"));

        Assert.Equal(ApiErrorKind.NetworkUnavailable, ex.Kind);
        Assert.DoesNotContain("HttpRequestException", ex.Message); // user-facing message, not a raw exception dump
    }

    [Fact]
    public async Task Genuine_timeout_throws_NetworkUnavailable()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueTimeout();
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.LoginAsync("a@b.com", "password1"));

        Assert.Equal(ApiErrorKind.NetworkUnavailable, ex.Kind);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_converted_to_NetworkUnavailable()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // The caller cancelled its own token - this must surface as a
        // cancellation, not be reinterpreted as "server unreachable".
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.LoginAsync("a@b.com", "password1", ct: cts.Token));
    }
}
