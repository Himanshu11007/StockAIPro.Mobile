using StockAIPro.Mobile.Services.Authentication;

namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>In-memory IPinStore for tests - the real SecurePinStore wraps
/// MAUI SecureStorage, which needs a platform context unavailable in a
/// plain unit test.</summary>
public sealed class InMemoryPinStore : IPinStore
{
    private PinRecord? _record;

    public Task<PinRecord?> GetAsync() => Task.FromResult(_record);

    public Task SaveAsync(PinRecord record)
    {
        _record = record;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _record = null;
        return Task.CompletedTask;
    }
}
