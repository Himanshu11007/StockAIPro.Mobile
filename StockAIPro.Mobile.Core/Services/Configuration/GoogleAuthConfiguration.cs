namespace StockAIPro.Mobile.Services.Configuration;

/// <summary>
/// Google Sign-In's "server client ID" - the OAuth 2.0 Web-application
/// client ID registered in Google Cloud Console. This must be the exact
/// same value as the backend's GOOGLE_OAUTH_CLIENT_ID (see
/// auth/external_identity.py and docs/AUTHENTICATION.md on the backend) -
/// it is the `aud` (audience) every Google id_token this app obtains will
/// carry, and the backend only accepts tokens whose audience matches its
/// own configured value.
///
/// PUBLIC CLIENT CONFIGURATION, not a secret: Google's own Credential
/// Manager / "Sign in with Google" documentation has this value compiled
/// directly into the app, the same way a Firebase google-services.json API
/// key is public - it identifies which backend a token was issued for, it
/// does not grant access by itself. A SEPARATE Android OAuth client
/// (package name + release/debug signing-certificate SHA-1 fingerprint)
/// must also be registered in Google Cloud Console for Credential Manager
/// to authorize this specific app build at all; that registration is
/// entirely server-side (Google Cloud Console) and this app never embeds
/// anything for it.
///
/// Never put an OAuth CLIENT SECRET here or anywhere in this app - Android/
/// iOS/mobile OAuth client registrations in Google Cloud Console do not
/// issue one, and embedding a secret in a mobile binary would defeat its
/// purpose entirely (see docs/AUTHENTICATION.md).
/// </summary>
public static class GoogleAuthConfiguration
{
    /// <summary>Empty until a real Google Cloud OAuth Web-application
    /// client id is supplied. IGoogleSignInService implementations must
    /// treat an empty value as "not configured" and fail closed
    /// (GoogleSignInNotConfiguredException) rather than calling Google
    /// with an empty/placeholder audience.</summary>
    public const string ServerClientId = "";

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ServerClientId);
}
