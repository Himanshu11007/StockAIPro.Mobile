using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Authentication;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

/// <summary>Forgot / reset password: request mapping against the backend
/// contract (api/routes/auth_password.py) and AuthService behaviour.</summary>
public class PasswordResetTests
{
    private const string Generic = "If an account exists for this email address, a password reset link has been sent.";

    [Fact]
    public async Task ForgotPasswordAsync_posts_trimmed_email_json_and_returns_the_generic_message()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Accepted, JsonContent.Create(new { message = Generic }));
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        var message = await client.ForgotPasswordAsync("  user@example.com ");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"http://backend.test{ApiConfiguration.ApiPrefix}/auth/forgot-password", request.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.RequestBodies[0]!);
        Assert.Equal("user@example.com", body.RootElement.GetProperty("email").GetString());
        Assert.Null(request.Headers.Authorization); // unauthenticated client
        Assert.Equal(Generic, message);
    }

    [Fact]
    public async Task ResetPasswordAsync_posts_token_and_new_password()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new { message = "Your password has been reset." }));
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        await client.ResetPasswordAsync(" tok-123 ", "NewPassword1!");

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"http://backend.test{ApiConfiguration.ApiPrefix}/auth/reset-password", request.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.RequestBodies[0]!);
        Assert.Equal("tok-123", body.RootElement.GetProperty("token").GetString());
        Assert.Equal("NewPassword1!", body.RootElement.GetProperty("new_password").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ApiErrorKind.ValidationFailed, "This password reset link is invalid or has expired.")]
    [InlineData(HttpStatusCode.TooManyRequests, ApiErrorKind.TooManyRequests, "Too many password reset attempts. Please try again later.")]
    public async Task ResetPasswordAsync_surfaces_backend_errors(HttpStatusCode status, ApiErrorKind kind, string detail)
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(status, JsonContent.Create(new { detail }));
        var client = new AuthApiClient(new FakeHttpClientFactory(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.ResetPasswordAsync("tok", "NewPassword1!"));
        Assert.Equal(kind, ex.Kind);
        Assert.Equal(detail, ex.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_on_a_signed_in_device_clears_the_now_revoked_session()
    {
        var api = new FakeAuthApiClient();
        var tokens = new InMemoryTokenStore();
        var service = new AuthService(api, tokens, new InMemoryDeviceIdentityService());
        await service.LoginAsync("user@example.com", "OldPassword1!");
        Assert.True(service.IsAuthenticated);

        await service.ResetPasswordAsync("tok", "NewPassword1!");

        Assert.Equal(("tok", "NewPassword1!"), api.LastReset);
        Assert.False(service.IsAuthenticated);
        Assert.Null(await tokens.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task Failed_reset_keeps_the_current_session()
    {
        var api = new FakeAuthApiClient { ResetPasswordException = new ApiException(ApiErrorKind.ValidationFailed, "invalid") };
        var service = new AuthService(api, new InMemoryTokenStore(), new InMemoryDeviceIdentityService());
        await service.LoginAsync("user@example.com", "OldPassword1!");

        await Assert.ThrowsAsync<ApiException>(() => service.ResetPasswordAsync("tok", "NewPassword1!"));
        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_returns_the_backend_message()
    {
        var api = new FakeAuthApiClient();
        var service = new AuthService(api, new InMemoryTokenStore(), new InMemoryDeviceIdentityService());

        Assert.Equal(Generic, await service.RequestPasswordResetAsync("user@example.com"));
        Assert.Equal("user@example.com", api.LastForgotPasswordEmail);
    }
}
