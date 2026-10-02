namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Configurable local brute-force policy for PIN verification. A 4-digit
/// PIN has only 10,000 combinations, so unlimited local guessing would make
/// it trivially guessable even though it never leaves the device - this
/// policy bounds that without ever permanently locking the user out: the
/// escalating lockouts delay guessing, but "use another login method" (full
/// Google/Apple/OTP/password re-authentication) always remains available as
/// an immediate fallback, independent of any PIN lockout state.
/// </summary>
public sealed class PinLockoutPolicy
{
    /// <summary>Wrong attempts allowed before the first lockout kicks in.</summary>
    public int FreeAttempts { get; init; } = 3;

    /// <summary>Escalating lockout durations applied after FreeAttempts is
    /// exceeded - index 0 for the first lockout, then increasing; the last
    /// entry repeats for any further attempts rather than growing forever.</summary>
    public TimeSpan[] LockoutDurations { get; init; } =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
    ];

    public TimeSpan GetLockoutDuration(int failedAttemptsBeyondFree)
    {
        var index = Math.Min(Math.Max(failedAttemptsBeyondFree - 1, 0), LockoutDurations.Length - 1);
        return LockoutDurations[index];
    }
}
