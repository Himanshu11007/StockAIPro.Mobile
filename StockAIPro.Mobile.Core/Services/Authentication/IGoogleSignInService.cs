namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Triggers the platform's native Google Sign-In flow and returns the
/// resulting Google id_token for the backend to verify. The id_token is
/// NEVER trusted by this app or treated as proof of identity by itself -
/// it is only meaningful once the backend verifies its signature against
/// Google's own public keys (see auth/external_identity.py:
/// GoogleIdentityVerifier on the backend).
/// </summary>
public interface IGoogleSignInService
{
    /// <summary>Returns the Google id_token on success, or null if the user
    /// cancelled the sign-in flow before completing it. Throws
    /// GoogleSignInNotConfiguredException if no native Google Sign-In
    /// client has been configured for this app build.</summary>
    Task<string?> SignInAsync(CancellationToken ct = default);
}

/// <summary>
/// Thrown by the default (unconfigured) IGoogleSignInService: real Google
/// Sign-In requires a native SDK plus a registered OAuth client id (see
/// docs/AUTHENTICATION.md "Google setup"), neither of which exists in this
/// build. This is intentionally a loud failure rather than a fake success -
/// no Google identity token is ever fabricated client-side.
/// </summary>
public sealed class GoogleSignInNotConfiguredException() : Exception(
    "Google Sign-In isn't set up yet for this app. See docs/AUTHENTICATION.md \"Google setup\".");

/// <summary>
/// Thrown when the native Google Sign-In flow itself fails for a reason
/// other than the user cancelling (cancellation returns null instead - see
/// IGoogleSignInService.SignInAsync) or "not configured" - e.g. no Google
/// account on the device, Play Services unavailable, or a transient
/// Credential Manager error. The message is always safe to show directly
/// to the user (never a raw provider/Java exception message).
/// </summary>
public sealed class GoogleSignInFailedException(string message, Exception? inner = null)
    : Exception(message, inner);
