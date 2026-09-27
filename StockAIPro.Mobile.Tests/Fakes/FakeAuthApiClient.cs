using StockAIPro.Mobile.Models.Auth;
using StockAIPro.Mobile.Services.Api;

namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>Configurable IAuthApiClient test double - records calls and lets
/// each method's behavior be swapped per test via a delegate.</summary>
public sealed class FakeAuthApiClient : IAuthApiClient
{
    public int RegisterCallCount { get; private set; }
    public int LoginCallCount { get; private set; }
    public int RefreshCallCount { get; private set; }
    public int LogoutCallCount { get; private set; }
    public int GetCurrentUserCallCount { get; private set; }

    public List<string> RefreshTokensUsed { get; } = [];
    public List<string> LogoutTokensUsed { get; } = [];
    public (string Email, string Password)? LastLoginArgs { get; private set; }
    public (string Email, string Password)? LastRegisterArgs { get; private set; }

    public Func<TokenResponse> RegisterResult { get; set; } = () => Canned.Tokens();
    public Func<TokenResponse> LoginResult { get; set; } = () => Canned.Tokens();
    public Func<TokenResponse> RefreshResult { get; set; } = () => Canned.Tokens();
    public Func<UserProfileResponse> CurrentUserResult { get; set; } = () => Canned.Profile();

    public Task<TokenResponse> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        RegisterCallCount++;
        LastRegisterArgs = (email, password);
        return Task.FromResult(RegisterResult());
    }

    public Task<TokenResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        LoginCallCount++;
        LastLoginArgs = (email, password);
        return Task.FromResult(LoginResult());
    }

    public Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        RefreshCallCount++;
        RefreshTokensUsed.Add(refreshToken);
        return Task.FromResult(RefreshResult());
    }

    public Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        LogoutCallCount++;
        LogoutTokensUsed.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken ct = default)
    {
        GetCurrentUserCallCount++;
        return Task.FromResult(CurrentUserResult());
    }
}

public static class Canned
{
    public static TokenResponse Tokens(string accessToken = "access-token", string refreshToken = "refresh-token") =>
        new() { AccessToken = accessToken, RefreshToken = refreshToken, TokenType = "bearer" };

    public static UserProfileResponse Profile(string email = "user@example.com") => new()
    {
        Id = 1,
        Email = email,
        IsActive = true,
        Roles = ["USER"],
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
