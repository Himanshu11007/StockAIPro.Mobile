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
    private int _getSnapshotCallCount;
    private int _sessionCounter;

    public int ClearCallCount { get; private set; }
    public int SaveCallCount { get; private set; }

    /// <summary>Invoked (with the 1-based call index) just before
    /// GetSnapshotAsync returns - lets tests simulate a concurrent logout
    /// and/or login (account switch or same-account rotation) landing
    /// between two specific snapshot reads (e.g. the snapshot taken when a
    /// request is first sent vs. the re-read after acquiring the refresh
    /// lock). Because the fake mutates its fields before returning the
    /// snapshot captured from them, a hook that calls ClearAsync/
    /// SaveTokensAsync here reproduces "the switch fully completed before
    /// this atomic read", matching what a real lock-protected store would
    /// also observe.</summary>
    public Action<int>? OnGetSnapshot { get; set; }

    public InMemoryTokenStore() { }

    public InMemoryTokenStore(string accessToken, string refreshToken)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _sessionId = NewSessionId();
    }

    public Task<string?> GetAccessTokenAsync() => Task.FromResult(_accessToken);

    public Task<string?> GetRefreshTokenAsync() => Task.FromResult(_refreshToken);

    public Task<string?> GetSessionIdAsync() => Task.FromResult(_sessionId);

    public Task<TokenSessionSnapshot> GetSnapshotAsync()
    {
        var callIndex = ++_getSnapshotCallCount;
        OnGetSnapshot?.Invoke(callIndex);
        return Task.FromResult(new TokenSessionSnapshot(_accessToken, _refreshToken, _sessionId));
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
