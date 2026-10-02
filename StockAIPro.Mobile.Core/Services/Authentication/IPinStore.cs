namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// What's persisted for a local PIN: a per-device random salt and the
/// resulting PBKDF2 hash (see PinHasher) - never the PIN itself - plus
/// local brute-force tracking (FailedAttempts/LockedUntil). All four fields
/// live together as one record so they can only ever be read/written
/// atomically as a unit.
/// </summary>
public sealed record PinRecord(string Salt, string Hash, int FailedAttempts, DateTimeOffset? LockedUntil);

/// <summary>
/// Persists the local PIN's verification material. The one implementation
/// shipped (SecurePinStore) uses the same MAUI platform secure storage
/// SecureTokenStore uses for tokens - never plain files, Preferences, or
/// app state. Abstracted as an interface so PinService can be unit tested
/// with an in-memory fake.
/// </summary>
public interface IPinStore
{
    Task<PinRecord?> GetAsync();
    Task SaveAsync(PinRecord record);
    Task ClearAsync();
}
