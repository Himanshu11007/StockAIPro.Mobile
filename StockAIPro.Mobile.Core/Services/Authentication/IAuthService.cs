using StockAIPro.Mobile.Models.Auth;

namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Application-level authentication state + orchestration. Razor components
/// read IsAuthenticated/CurrentUser and subscribe to AuthStateChanged rather
/// than talking to IAuthApiClient directly.
///
/// Client-side roles (CurrentUser?.Roles) are for UI presentation only
/// (e.g. showing/hiding a menu entry) - they are never used to grant access
/// to anything the backend itself would reject. The backend remains the
/// sole authority for what a request is actually allowed to do.
/// </summary>
public interface IAuthService
{
    bool IsAuthenticated { get; }
    UserProfileResponse? CurrentUser { get; }

    event Action? AuthStateChanged;

    Task RegisterAsync(string email, string password, CancellationToken ct = default);
    Task LoginAsync(string email, string password, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<UserProfileResponse> RefreshCurrentUserAsync(CancellationToken ct = default);

    /// <summary>Called once at app startup. Returns true if a valid session
    /// was restored from stored credentials, false otherwise (no stored
    /// credentials, invalid/expired refresh token, or backend unreachable -
    /// callers should not treat "false" as necessarily an error to show).</summary>
    Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);
}
