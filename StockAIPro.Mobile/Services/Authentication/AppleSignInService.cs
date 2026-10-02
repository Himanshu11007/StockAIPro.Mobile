namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Default IAppleSignInService - fails closed. Replacing this with a real
/// implementation (wiring Sign in with Apple's native API and this app's
/// registered Services ID/entitlement) is a drop-in swap in
/// MauiProgram.cs's DI registration; nothing else in the app needs to
/// change, since every caller only ever depends on the IAppleSignInService
/// interface.
/// </summary>
public sealed class AppleSignInService : IAppleSignInService
{
    public Task<string?> SignInAsync(CancellationToken ct = default) =>
        throw new AppleSignInNotConfiguredException();
}
