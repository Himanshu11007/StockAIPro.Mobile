using System.Security.Cryptography;

namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Hashes a 4-digit PIN for local storage. A PIN has only 10,000 possible
/// values, so this is deliberately NOT treated as equivalent to a strong
/// password hash protecting a remote credential - it exists only to avoid
/// storing the raw PIN, as defense-in-depth alongside platform secure
/// storage (see IPinStore), not as the sole thing standing between an
/// attacker and the account. The actual security boundary is: (1) this
/// material never leaves the device, and (2) a correct PIN only unlocks a
/// refresh token that is re-validated against the server immediately after
/// (see PinService / AuthenticatedHttpMessageHandler) - it is never treated
/// as proof of identity by the backend.
/// </summary>
public static class PinHasher
{
    private const int Iterations = 100_000;
    private const int KeyLengthBytes = 32;

    public static string NewSalt() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public static string Hash(string pin, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var derived = Rfc2898DeriveBytes.Pbkdf2(pin, saltBytes, Iterations, HashAlgorithmName.SHA256, KeyLengthBytes);
        return Convert.ToBase64String(derived);
    }

    /// <summary>Constant-time comparison - a PIN check must not leak timing
    /// information about how many leading bytes of the hash matched.</summary>
    public static bool Verify(string pin, string salt, string expectedHash)
    {
        var actualHash = Convert.FromBase64String(Hash(pin, salt));
        var expected = Convert.FromBase64String(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actualHash, expected);
    }
}
