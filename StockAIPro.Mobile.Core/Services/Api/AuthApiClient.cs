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

    public async Task<TokenResponse> RegisterAsync(
        string email, string password, string? deviceId = null, string? deviceName = null, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/register",
                new RegisterRequest { Email = email, Password = password, DeviceId = deviceId, DeviceName = deviceName }, ct),
            ct);
        return await ReadOrThrowAsync<TokenResponse>(response, ct);
    }

    public async Task<TokenResponse> LoginAsync(
        string email, string password, string? deviceId = null, string? deviceName = null, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        // Backend uses OAuth2's password flow (FastAPI OAuth2PasswordRequestForm),
        // which requires application/x-www-form-urlencoded, not JSON - see
        // api/routes/auth.py:login()'s own docstring.
        var fields = new Dictionary<string, string>
        {
            ["username"] = email,
            ["password"] = password,
        };
        if (!string.IsNullOrEmpty(deviceId))
            fields["device_id"] = deviceId;
        if (!string.IsNullOrEmpty(deviceName))
            fields["device_name"] = deviceName;
        var form = new FormUrlEncodedContent(fields);
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

    // ── Phase 8: Google / Apple / OTP sign-in ───────────────────────────────

    public async Task<TokenResponse> LoginWithGoogleAsync(
        string idToken, string? deviceId, string? deviceName, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/google",
                new GoogleAuthRequest { IdToken = idToken, DeviceId = deviceId, DeviceName = deviceName }, ct),
            ct);
        return await ReadOrThrowAsync<TokenResponse>(response, ct);
    }

    public async Task<TokenResponse> LoginWithAppleAsync(
        string identityToken, string? deviceId, string? deviceName, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/apple",
                new AppleAuthRequest { IdentityToken = identityToken, DeviceId = deviceId, DeviceName = deviceName }, ct),
            ct);
        return await ReadOrThrowAsync<TokenResponse>(response, ct);
    }

    public async Task RequestOtpAsync(string destination, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/otp/request",
                new OtpRequestRequest { Destination = destination }, ct),
            ct);
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);
    }

    public async Task<TokenResponse> VerifyOtpAsync(
        string destination, string code, string? deviceId, string? deviceName, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/otp/verify",
                new OtpVerifyRequest { Destination = destination, Code = code, DeviceId = deviceId, DeviceName = deviceName }, ct),
            ct);
        return await ReadOrThrowAsync<TokenResponse>(response, ct);
    }

    // ── Phase 8: account linking, device/session management ────────────────

    public async Task<LinkedIdentityResponse> LinkGoogleAsync(string idToken, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/link/google", new LinkGoogleRequest { IdToken = idToken }, ct),
            ct);
        return await ReadOrThrowAsync<LinkedIdentityResponse>(response, ct);
    }

    public async Task<LinkedIdentityResponse> LinkAppleAsync(string identityToken, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/link/apple", new LinkAppleRequest { IdentityToken = identityToken }, ct),
            ct);
        return await ReadOrThrowAsync<LinkedIdentityResponse>(response, ct);
    }

    public async Task<List<LinkedIdentityResponse>> GetLinkedIdentitiesAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/auth/identities", ct), ct);
        return await ReadOrThrowAsync<List<LinkedIdentityResponse>>(response, ct);
    }

    public async Task UnlinkIdentityAsync(string provider, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.DeleteAsync($"{ApiConfiguration.ApiPrefix}/auth/identities/{provider}", ct), ct);
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);
    }

    public async Task<List<SessionResponse>> GetSessionsAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/auth/sessions", ct), ct);
        return await ReadOrThrowAsync<List<SessionResponse>>(response, ct);
    }

    public async Task RevokeSessionAsync(int sessionId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/sessions/revoke", new RevokeSessionRequest { SessionId = sessionId }, ct),
            ct);
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);
    }

    public async Task<RevokeAllSessionsResponse> RevokeAllSessionsAsync(
        bool exceptCurrent, string? currentDeviceId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/sessions/revoke-all",
                new RevokeAllSessionsRequest { ExceptCurrent = exceptCurrent, CurrentDeviceId = currentDeviceId }, ct),
            ct);
        return await ReadOrThrowAsync<RevokeAllSessionsResponse>(response, ct);
    }

    public async Task SetPinEnabledAsync(string deviceId, bool enabled, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await SendAsync(
            client, () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/auth/devices/pin-enabled",
                new SetPinEnabledRequest { DeviceId = deviceId, Enabled = enabled }, ct),
            ct);
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);
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
