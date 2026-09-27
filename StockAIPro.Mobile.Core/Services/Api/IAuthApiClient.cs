using StockAIPro.Mobile.Models.Auth;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// Raw calls to /api/v1/auth/* - exactly the six endpoints that exist on
/// the backend today (api/routes/auth.py). No token-refresh/retry logic
/// lives here; that belongs to Services/Authentication (which uses this
/// client, notably for the refresh call itself - keeping this client
/// "dumb" avoids a circular dependency between the two).
/// </summary>
public interface IAuthApiClient
{
    Task<TokenResponse> RegisterAsync(string email, string password, CancellationToken ct = default);

    /// <summary>OAuth2 password flow: backend expects form fields
    /// username/password, not JSON - see Body_login_api_v1_auth_login_post
    /// in api/schemas_auth.py / FastAPI's OAuth2PasswordRequestForm.</summary>
    Task<TokenResponse> LoginAsync(string email, string password, CancellationToken ct = default);

    Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Backend returns 204 No Content on success - nothing to
    /// return. Caller clears local credentials regardless of outcome.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// GET /auth/me. Unlike the other methods here, this one is routed
    /// through the authenticated HTTP pipeline (Services/Authentication) so
    /// the Authorization header is attached automatically and a 401 can
    /// trigger the same refresh-and-retry-once flow as any other
    /// authenticated call - this IS the "real authenticated request" the
    /// Phase 7A objective asks for, not a separate demo call.
    /// </summary>
    Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken ct = default);
}
