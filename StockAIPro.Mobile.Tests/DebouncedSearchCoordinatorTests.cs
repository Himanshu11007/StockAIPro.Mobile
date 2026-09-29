using StockAIPro.Mobile.Services.Search;

namespace StockAIPro.Mobile.Tests;

/// <summary>
/// These tests are deliberately synchronous (no `async Task`, no
/// `Task.Delay`-based waiting) - ordering is proven by construction, not by
/// timing:
///   - ImmediateDelay never yields (awaiting Task.CompletedTask resumes
///     inline), so Start(...) always progresses synchronously all the way
///     into the fake search call before returning to the caller.
///   - A plain TaskCompletionSource (no RunContinuationsAsynchronously)
///     resumes its awaiter's continuation synchronously, on the calling
///     thread, the moment SetResult/SetException/TrySetCanceled is called -
///     both documented, relied-upon TPL behaviors, not incidental timing.
/// That combination means every "who wins the race" outcome below is fully
/// deterministic the instant the relevant TaskCompletionSource is resolved,
/// with no need to wait for anything.
/// </summary>
public class DebouncedSearchCoordinatorTests
{
    private static Task ImmediateDelay(TimeSpan _, CancellationToken ct) => Task.CompletedTask;

    [Fact]
    public void Newer_search_result_wins_even_if_the_older_search_completes_later()
    {
        var completeA = new TaskCompletionSource<string>();
        var completeB = new TaskCompletionSource<string>();
        var delivered = new List<string>();

        Task<string> Search(string query, CancellationToken ct) => query == "A" ? completeA.Task : completeB.Task;

        var coordinator = new DebouncedSearchCoordinator<string>(Search, TimeSpan.Zero, ImmediateDelay);

        coordinator.Start("A", delivered.Add, _ => { });
        coordinator.Start("B", delivered.Add, _ => { }); // supersedes A before A has resolved

        // Resolve the OLDER call after the newer one is already the active
        // one - exactly the "TATA resolves after TATAMOTORS" race from the
        // corrective-hardening task.
        completeA.SetResult("stale-A-result");
        completeB.SetResult("fresh-B-result");

        Assert.Equal(new[] { "fresh-B-result" }, delivered); // A's stale result must never be delivered
    }

    [Fact]
    public void A_failed_stale_search_never_surfaces_an_error_over_a_newer_successful_one()
    {
        var failA = new TaskCompletionSource<string>();
        var completeB = new TaskCompletionSource<string>();
        var successes = new List<string>();
        var failures = new List<Exception>();

        Task<string> Search(string query, CancellationToken ct) => query == "A" ? failA.Task : completeB.Task;

        var coordinator = new DebouncedSearchCoordinator<string>(Search, TimeSpan.Zero, ImmediateDelay);

        coordinator.Start("A", successes.Add, failures.Add);
        coordinator.Start("B", successes.Add, failures.Add);

        failA.SetException(new InvalidOperationException("stale failure"));
        completeB.SetResult("fresh-B-result");

        Assert.Equal(new[] { "fresh-B-result" }, successes);
        Assert.Empty(failures); // A's stale failure must never be surfaced
    }

    [Fact]
    public void Cancelling_during_the_debounce_delay_never_calls_search()
    {
        var callCount = 0;
        Task<string> Search(string query, CancellationToken ct)
        {
            callCount++;
            return Task.FromResult(query);
        }

        // A delay that only ever completes via cancellation - deterministically
        // simulates "still within the debounce window" without depending on
        // wall-clock timing at all. CancellationTokenSource.Cancel() invokes
        // Register()'d callbacks synchronously on the calling thread, so
        // this resolves inline the moment the newer Start() cancels it.
        Task NeverElapsingDelay(TimeSpan _, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource();
            ct.Register(() => tcs.TrySetCanceled(ct));
            return tcs.Task;
        }

        var coordinator = new DebouncedSearchCoordinator<string>(Search, TimeSpan.FromSeconds(1), NeverElapsingDelay);

        coordinator.Start("A", _ => { }, _ => { });
        coordinator.Start("B", _ => { }, _ => { }); // synchronously cancels A's still-pending delay

        Assert.Equal(0, callCount); // neither "A" (cancelled) nor "B" (still debouncing) ever reached search
    }

    [Fact]
    public void Cancelling_an_in_flight_search_never_delivers_a_result_or_error()
    {
        var completeA = new TaskCompletionSource<string>();
        var successCount = 0;
        var failureCount = 0;

        Task<string> Search(string query, CancellationToken ct) => completeA.Task;

        var coordinator = new DebouncedSearchCoordinator<string>(Search, TimeSpan.Zero, ImmediateDelay);

        coordinator.Start("A", _ => successCount++, _ => failureCount++);
        coordinator.CancelPending(); // supersedes A without starting a new search
        completeA.SetResult("late-result-after-cancel");

        Assert.Equal(0, successCount);
        Assert.Equal(0, failureCount);
    }
}
