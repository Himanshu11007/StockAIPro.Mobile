namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Triggers Sign in with Apple and returns the resulting Apple
/// identity_token for the backend to verify. Never trusted by this app
/// itself - only meaningful once the backend verifies its signature against
/// Apple's own public keys (see auth/external_identity.py:
/// AppleIdentityVerifier on the backend).
/// </summary>
public interface IAppleSignInService
{
    /// <summary>Returns the Apple identity_token on success, or null if the
    /// user cancelled the sign-in flow before completing it. Throws
    /// AppleSignInNotConfiguredException if the Sign in with Apple
    /// capability/Services ID hasn't been configured for this app build.</summary>
    Task<string?> SignInAsync(CancellationToken ct = default);
}

/// <summary>
/// Thrown by the default (unconfigured) IAppleSignInService: real Sign in
/// with Apple requires an Apple Developer account, a registered Services
/// ID, and the capability enabled in the app's entitlements (see
/// docs/AUTHENTICATION.md "Apple setup"), none of which exist in this
/// build. This is intentionally a loud failure rather than a fake success -
/// no Apple identity token is ever fabricated client-side.
/// </summary>
public sealed class AppleSignInNotConfiguredException() : Exception(
    "Sign in with Apple isn't set up yet for this app. See docs/AUTHENTICATION.md \"Apple setup\".");
