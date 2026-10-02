using Microsoft.Maui.Authentication;

namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Real iOS IAppleSignInService, using .NET MAUI's built-in native Sign in
/// with Apple support (Microsoft.Maui.Authentication.AppleSignInAuthenticator -
/// ships as part of the MAUI SDK itself, no extra NuGet package needed).
/// This wraps Apple's own AuthenticationServices framework
/// (ASAuthorizationAppleIdProvider/ASAuthorizationController) under the
/// hood - the same supported mechanism Apple's own documentation describes
/// for native apps, not a browser-based workaround.
///
/// Requires the com.apple.developer.applesignin entitlement (see
/// Platforms/iOS/Entitlements.plist) AND the "Sign In with Apple"
/// capability enabled for this app's App ID in the Apple Developer portal -
/// see docs/AUTHENTICATION.md "Apple setup". Without both, the native
/// authorization request fails.
/// </summary>
public sealed class AppleSignInService : IAppleSignInService
{
    public async Task<string?> SignInAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await AppleSignInAuthenticator.AuthenticateAsync(new AppleSignInAuthenticator.Options
            {
                IncludeEmailScope = true,
                IncludeFullNameScope = true,
            });

            // Apple's flow populates IdToken, not AccessToken (see
            // Microsoft's own WebAuthenticatorResult docs) - this is the
            // OIDC identity_token the backend verifies against Apple's own
            // public keys (auth/external_identity.py:AppleIdentityVerifier).
            // Any name/email in result.Properties is first-login-only
            // client-reported metadata and is NEVER sent to the backend as
            // proof of identity - only the verified identity_token is.
            return result.IdToken;
        }
        catch (TaskCanceledException)
        {
            return null; // user cancelled the native sign-in flow
        }
        catch (Exception ex) when (ex is not AppleSignInFailedException)
        {
            throw new AppleSignInFailedException("Apple sign-in is currently unavailable. Please try again.", ex);
        }
    }
}
