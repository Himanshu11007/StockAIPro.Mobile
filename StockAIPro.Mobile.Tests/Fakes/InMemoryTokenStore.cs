using StockAIPro.Mobile.Services.Authentication;

namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>In-memory ITokenStore for tests - the real SecureTokenStore
/// wraps MAUI SecureStorage, which needs a platform context unavailable in
/// a plain unit test.</summary>
public sealed class InMemoryTokenStore : ITokenStore
{
    private string? _accessToken;
    private string? _refreshToken;
    private string? _sessionId;
    private int _getRefreshTokenCallCount;
    private int _getSessionIdCallCount;
    private int _sessionCounter;

    public int ClearCallCount { get; private set; }
    public int SaveCallCount { get; private set; }

    /// <summary>Invoked (with the 1-based call index) just before
    /// GetRefreshTokenAsync returns - lets tests simulate a concurrent
    /// logout/rotation landing between two specific reads of the refresh
    /// token (e.g. the read before acquiring the refresh lock vs. the
    /// re-read after acquiring it).</summary>
    public Action<int>? OnGetRefreshToken { get; set; }

    /// <summary>Invoked (with the 1-based call index) just before
    /// GetSessionIdAsync returns - lets tests simulate an account switch
    /// (logout, or logout-then-login as a different/same account) landing
    /// between two specific reads of the session id (e.g. the read taken
    /// when a request is first sent vs. the re-read after acquiring the
    /// refresh lock).</summary>
    public Action<int>? OnGetSessionId { get; set; }

    public InMemoryTokenStore() { }

    public InMemoryTokenStore(string accessToken, string refreshToken)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _sessionId = NewSessionId();
    }

    public Task<string?> GetAccessTokenAsync() => Task.FromResult(_accessToken);

    public Task<string?> GetRefreshTokenAsync()
    {
        var callIndex = ++_getRefreshTokenCallCount;
        OnGetRefreshToken?.Invoke(callIndex);
        return Task.FromResult(_refreshToken);
    }

    public Task<string?> GetSessionIdAsync()
    {
        var callIndex = ++_getSessionIdCallCount;
        OnGetSessionId?.Invoke(callIndex);
        return Task.FromResult(_sessionId);
    }

    public Task SaveTokensAsync(string accessToken, string refreshToken, bool isNewSession = false)
    {
        SaveCallCount++;
        if (isNewSession)
            _sessionId = NewSessionId();
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        ClearCallCount++;
        _accessToken = null;
        _refreshToken = null;
        _sessionId = null;
        return Task.CompletedTask;
    }

    /// <summary>Deterministic, test-friendly stand-in for the real store's
    /// Guid.NewGuid() - readable in assertions/failure output and still
    /// guaranteed distinct per call within a test.</summary>
    private string NewSessionId() => $"session-{++_sessionCounter}";
}
