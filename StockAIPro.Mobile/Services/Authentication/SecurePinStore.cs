using System.Text.Json;

namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// IPinStore backed by Microsoft.Maui.Storage.SecureStorage - same
/// mechanism SecureTokenStore uses for tokens. Stores only the salted hash
/// plus lockout bookkeeping (see PinRecord) - never the PIN itself.
/// </summary>
public sealed class SecurePinStore : IPinStore
{
    private const string PinRecordKey = "stockaipro_pin_record";

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<PinRecord?> GetAsync()
    {
        var json = await SecureStorage.Default.GetAsync(PinRecordKey);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PinRecord>(json);
    }

    public async Task SaveAsync(PinRecord record)
    {
        await _lock.WaitAsync();
        try
        {
            await SecureStorage.Default.SetAsync(PinRecordKey, JsonSerializer.Serialize(record));
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(PinRecordKey);
        return Task.CompletedTask;
    }
}
