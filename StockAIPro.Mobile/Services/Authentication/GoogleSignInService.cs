namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Default IGoogleSignInService - fails closed. Replacing this with a real
/// implementation (wiring a native Google Sign-In SDK and this app's
/// registered OAuth client id) is a drop-in swap in MauiProgram.cs's DI
/// registration; nothing else in the app needs to change, since every
/// caller only ever depends on the IGoogleSignInService interface.
/// </summary>
public sealed class GoogleSignInService : IGoogleSignInService
{
    public Task<string?> SignInAsync(CancellationToken ct = default) =>
        throw new GoogleSignInNotConfiguredException();
}
