#if !IOS
namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Default IAppleSignInService for platforms without a real implementation
/// (Android/MacCatalyst/Windows - Sign in with Apple is an iOS-only
/// requirement per this task's scope) - fails closed. iOS has its own real
/// implementation at Platforms/iOS/AppleSignInService.cs
/// (AppleSignInAuthenticator), registered instead of this one when building
/// for iOS - see the #if IOS guard here and MauiProgram.cs.
/// </summary>
public sealed class AppleSignInService : IAppleSignInService
{
    public Task<string?> SignInAsync(CancellationToken ct = default) =>
        throw new AppleSignInNotConfiguredException();
}
#endif
