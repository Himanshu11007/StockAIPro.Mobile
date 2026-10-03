using System.Net.Http.Json;
using StockAIPro.Mobile.Models.Common;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// Shared transport/error handling for the business API clients (stocks,
/// watchlist, analysis, top picks, performance, intelligence) - everything
/// under /api/v1 except /auth/*. Mirrors AuthApiClient's own private
/// SendAsync/ReadOrThrowAsync helpers exactly (same network/cancellation
/// mapping, same non-success -> ApiException translation), factored out
/// here because six different clients need the identical logic, whereas
/// AuthApiClient's raw (non-enveloped) response shape is unique to /auth/*.
/// </summary>
internal static class BusinessApiSend
{
    public static async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            return await send();
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            // TaskCanceledException without the caller's own token being
            // cancelled means it was OUR timeout, not a user-initiated
            // cancellation. If ct.IsCancellationRequested is true, this
            // filter does not match, so the original exception propagates
            // unchanged - caller cancellation must never be reported as a
            // network error.
            throw ApiException.NetworkUnavailable(ex);
        }
    }

    /// <summary>Unwraps the {"success": true, "data": ..., "message": ...}
    /// envelope every business endpoint returns on success, or throws the
    /// appropriately-typed ApiException for a non-success response.</summary>
    public static async Task<T> ReadDataOrThrowAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(cancellationToken: ct);
        if (envelope is null)
            throw new ApiException(ApiErrorKind.Unknown, "The server returned an empty response.");
        return envelope.Data;
    }

    /// <summary>Like ReadDataOrThrowAsync but also returns the envelope's
    /// user-facing message (e.g. a report receipt).</summary>
    public static async Task<(T Data, string Message)> ReadEnvelopeOrThrowAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(cancellationToken: ct);
        if (envelope is null)
            throw new ApiException(ApiErrorKind.Unknown, "The server returned an empty response.");
        return (envelope.Data, envelope.Message);
    }

    /// <summary>Non-success -> ApiException; success body ignored (204 etc.).</summary>
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);
    }

    /// <summary>For the handful of routes that use `response_model=...`
    /// directly instead of success_envelope(...) - namely /stocks and
    /// /watchlist (see api/routes/stocks.py and api/routes/watchlist.py,
    /// neither of which calls success_envelope) - the JSON body IS the
    /// model, with no {"success", "data", "message"} wrapper.</summary>
    public static async Task<T> ReadRawOrThrowAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return result ?? throw new ApiException(ApiErrorKind.Unknown, "The server returned an empty response.");
    }
}
