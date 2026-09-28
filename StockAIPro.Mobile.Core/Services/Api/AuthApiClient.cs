using System.Net.Http.Json;
using StockAIPro.Mobile.Models.Auth;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

public sealed class AuthApiClient : IAuthApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TokenResponse> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/register",
                new RegisterRequest { Email = email, Password = password }, ct),
            ct);
        return await ReadOrThrowAsync<TokenResponse>(response, ct);
    }

    public async Task<TokenResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        // Backend uses OAuth2's password flow (FastAPI OAuth2PasswordRequestForm),
        // which requires application/x-www-form-urlencoded, not JSON - see
        // api/routes/auth.py:login()'s own docstring.
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = email,
            ["password"] = password,
        });
        using var response = await SendAsync(
            client, () => client.PostAsync($"{ApiConfiguration.ApiPrefix}/auth/login", form, ct), ct);
        return await ReadOrThrowAsync<TokenResponse>(response, ct);
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/refresh",
                new RefreshRequest { RefreshToken = refreshToken }, ct),
            ct);
        return await ReadOrThrowAsync<TokenResponse>(response, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/logout",
                new LogoutRequest { RefreshToken = refreshToken }, ct),
            ct);
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);
    }

    public async Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/auth/me", ct), ct);
        return await ReadOrThrowAsync<UserProfileResponse>(response, ct);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            return await send();
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            // TaskCanceledException without the caller's own token being
            // cancelled means it was OUR timeout, not a user-initiated
            // cancellation - both cases mean "couldn't reach the server".
            // If ct.IsCancellationRequested is true, this filter does NOT
            // match, so the original OperationCanceledException/
            // TaskCanceledException propagates unchanged - caller-initiated
            // cancellation must never be reported as a network error.
            throw ApiException.NetworkUnavailable(ex);
        }
    }

    private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<T>(ct);
        return result ?? throw new ApiException(ApiErrorKind.Unknown, "The server returned an empty response.");
    }
}
