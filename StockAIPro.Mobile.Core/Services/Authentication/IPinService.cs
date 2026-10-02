namespace StockAIPro.Mobile.Services.Authentication;

public enum PinVerifyOutcome
{
    Success,
    WrongPin,
    LockedOut,
    NoPinSet,
}

public sealed record PinVerifyResult(PinVerifyOutcome Outcome, int? AttemptsRemaining = null, TimeSpan? LockoutRemaining = null);

/// <summary>
/// Local, on-device PIN setup/verification. This is NOT a remote
/// authentication mechanism and intentionally has no dependency on
/// IAuthService/IAuthApiClient - a correct PIN only ever proves "whoever is
/// holding this device knows the 4 digits," nothing more. The caller
/// (e.g. the PIN-unlock page) is responsible for following a Success
/// result with IAuthService.TryRestoreSessionAsync() to confirm the
/// server-side session this device already holds is still valid - see
/// docs/AUTHENTICATION.md's PIN security model. A PIN can never bypass
/// that check.
/// </summary>
public interface IPinService
{
    Task<bool> IsPinSetAsync();

    /// <summary>Throws ArgumentException if pin isn't exactly 4 digits, or
    /// if confirmPin doesn't match.</summary>
    Task SetPinAsync(string pin, string confirmPin);

    Task<PinVerifyResult> VerifyPinAsync(string pin);

    Task ClearPinAsync();
}

public sealed class PinService : IPinService
{
    private readonly IPinStore _pinStore;
    private readonly PinLockoutPolicy _policy;

    public PinService(IPinStore pinStore, PinLockoutPolicy? policy = null)
    {
        _pinStore = pinStore;
        _policy = policy ?? new PinLockoutPolicy();
    }

    public async Task<bool> IsPinSetAsync() => await _pinStore.GetAsync() is not null;

    public async Task SetPinAsync(string pin, string confirmPin)
    {
        if (!IsValidFormat(pin))
            throw new ArgumentException("PIN must be exactly 4 digits", nameof(pin));
        if (!string.Equals(pin, confirmPin, StringComparison.Ordinal))
            throw new ArgumentException("PIN confirmation does not match", nameof(confirmPin));

        var salt = PinHasher.NewSalt();
        var hash = PinHasher.Hash(pin, salt);
        await _pinStore.SaveAsync(new PinRecord(salt, hash, FailedAttempts: 0, LockedUntil: null));
    }

    public async Task<PinVerifyResult> VerifyPinAsync(string pin)
    {
        var record = await _pinStore.GetAsync();
        if (record is null)
            return new PinVerifyResult(PinVerifyOutcome.NoPinSet);

        var now = DateTimeOffset.UtcNow;
        if (record.LockedUntil is { } lockedUntil && lockedUntil > now)
            return new PinVerifyResult(PinVerifyOutcome.LockedOut, LockoutRemaining: lockedUntil - now);

        if (IsValidFormat(pin) && PinHasher.Verify(pin, record.Salt, record.Hash))
        {
            // Correct PIN - reset failure tracking.
            await _pinStore.SaveAsync(record with { FailedAttempts = 0, LockedUntil = null });
            return new PinVerifyResult(PinVerifyOutcome.Success);
        }

        var failedAttempts = record.FailedAttempts + 1;
        if (failedAttempts <= _policy.FreeAttempts)
        {
            await _pinStore.SaveAsync(record with { FailedAttempts = failedAttempts });
            return new PinVerifyResult(PinVerifyOutcome.WrongPin, AttemptsRemaining: _policy.FreeAttempts - failedAttempts);
        }

        var duration = _policy.GetLockoutDuration(failedAttempts - _policy.FreeAttempts);
        var lockedUntilNew = now + duration;
        await _pinStore.SaveAsync(record with { FailedAttempts = failedAttempts, LockedUntil = lockedUntilNew });
        return new PinVerifyResult(PinVerifyOutcome.LockedOut, LockoutRemaining: duration);
    }

    public Task ClearPinAsync() => _pinStore.ClearAsync();

    private static bool IsValidFormat(string pin) => pin.Length == 4 && pin.All(char.IsAsciiDigit);
}
