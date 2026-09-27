using StockAIPro.Mobile.Services.Authentication;

namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>In-memory ITokenStore for tests - the real SecureTokenStore
/// wraps MAUI SecureStorage, which needs a platform context unavailable in
/// a plain unit test.</summary>
public sealed class InMemoryTokenStore : ITokenStore
{
    private string? _accessToken;
    private string? _refreshToken;

    public int ClearCallCount { get; private set; }
    public int SaveCallCount { get; private set; }

    public InMemoryTokenStore() { }

    public InMemoryTokenStore(string accessToken, string refreshToken)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
    }

    public Task<string?> GetAccessTokenAsync() => Task.FromResult(_accessToken);

    public Task<string?> GetRefreshTokenAsync() => Task.FromResult(_refreshToken);

    public Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        SaveCallCount++;
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        ClearCallCount++;
        _accessToken = null;
        _refreshToken = null;
        return Task.CompletedTask;
    }
}
