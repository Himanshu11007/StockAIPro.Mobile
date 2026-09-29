namespace StockAIPro.Mobile.Services.Search;

/// <summary>
/// Debounces rapid calls to an async search operation (e.g. a
/// search-as-you-type box) and guarantees that only the outcome of the
/// LATEST call started is ever delivered, even if an older call's response
/// arrives after a newer one.
///
/// Two independent mechanisms enforce this:
///   1. Starting a new search cancels the CancellationToken of whichever
///      search (still debouncing or already in flight) preceded it.
///   2. Defense in depth, since cancellation is cooperative and not always
///      observed immediately by the underlying call: a completed search's
///      outcome is only delivered if it is still the most recently started
///      search by the time it completes - a stale result is silently
///      dropped rather than handed to the caller.
///
/// Extracted out of StockSearch.razor so this coordination logic is
/// unit-testable directly: a Razor component cannot be unit tested in this
/// project's current test setup (StockAIPro.Mobile.Tests only references
/// StockAIPro.Mobile.Core, not the MAUI project - see that project's own
/// README comment on why a net10.0-android test assembly cannot run on
/// desktop at all). Deliberately not Blazor-specific and not generic over
/// anything except the result type, so it stays trivial to construct in a
/// plain xUnit test with a fake search function.
/// </summary>
public sealed class DebouncedSearchCoordinator<TResult>
{
    private readonly Func<string, CancellationToken, Task<TResult>> _search;
    private readonly TimeSpan _debounce;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private CancellationTokenSource? _current;

    public DebouncedSearchCoordinator(
        Func<string, CancellationToken, Task<TResult>> search,
        TimeSpan debounce,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _search = search;
        _debounce = debounce;
        // Overridable only so tests can substitute a controllable fake
        // instead of racing against the real clock - production code
        // always uses the real Task.Delay.
        _delay = delay ?? Task.Delay;
    }

    /// <summary>
    /// Cancels whichever search is currently pending (debouncing or in
    /// flight) and starts a new one for <paramref name="query"/>.
    /// <paramref name="onSuccess"/>/<paramref name="onFailure"/> are invoked
    /// with the outcome only if this call is still the most recently
    /// started search by the time it completes; a cancelled search invokes
    /// neither.
    /// </summary>
    public void Start(string query, Action<TResult> onSuccess, Action<Exception> onFailure)
    {
        _current?.Cancel();
        _current?.Dispose();
        var cts = new CancellationTokenSource();
        _current = cts;
        _ = RunAsync(query, cts, onSuccess, onFailure);
    }

    /// <summary>Cancels any pending/in-flight search without starting a new
    /// one - e.g. once the user has picked a result and the search no
    /// longer needs to keep running, or the owning component is disposed.</summary>
    public void CancelPending()
    {
        _current?.Cancel();
        _current?.Dispose();
        _current = null;
    }

    private async Task RunAsync(
        string query, CancellationTokenSource cts, Action<TResult> onSuccess, Action<Exception> onFailure)
    {
        try
        {
            await _delay(_debounce, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return; // superseded by a newer call before the debounce elapsed
        }

        TResult result;
        try
        {
            result = await _search(query, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return; // superseded while the search itself was in flight
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(cts, _current))
                onFailure(ex);
            return;
        }

        // Even with cancellation wired through, a response can arrive after
        // a newer search has already started (cancellation is cooperative,
        // not instantaneous) - only deliver it if nothing newer has
        // superseded this call in the meantime.
        if (ReferenceEquals(cts, _current))
            onSuccess(result);
    }
}
