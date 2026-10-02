#if !ANDROID
namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Default IGoogleSignInService for platforms without a real
/// implementation yet (iOS/MacCatalyst/Windows) - fails closed. Android has
/// its own real implementation at Platforms/Android/GoogleSignInService.cs
/// (Credential Manager), registered instead of this one when building for
/// Android - see the #if ANDROID guard here and MauiProgram.cs. Wiring a
/// real implementation for another platform is a drop-in swap in
/// MauiProgram.cs's DI registration; nothing else needs to change, since
/// every caller only ever depends on IGoogleSignInService.
/// </summary>
public sealed class GoogleSignInService : IGoogleSignInService
{
    public Task<string?> SignInAsync(CancellationToken ct = default) =>
        throw new GoogleSignInNotConfiguredException();
}
#endif
