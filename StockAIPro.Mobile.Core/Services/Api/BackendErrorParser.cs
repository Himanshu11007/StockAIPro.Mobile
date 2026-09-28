using System.Net;
using System.Text.Json;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// Turns a failed HttpResponseMessage into an ApiException.
///
/// Verified directly against the running backend - two distinct error
/// shapes actually occur, depending on how the route raised the error:
///   1. FastAPI's own HTTPException (auth routes; watchlist/stocks 400/404/409)
///      -> {"detail": "&lt;string&gt;"} or, for 422 validation errors,
///      {"detail": [{"loc": [...], "msg": "...", "type": "..."}, ...]}.
///   2. A plain ValueError/KeyError/Exception raised from the service layer
///      and caught by api/main.py's centralized handlers (analyze-stock,
///      top-picks, performance, intelligence) -> {"success": false,
///      "error": "&lt;category&gt;", "details": "&lt;specific message&gt;"}.
/// Both are handled here so every endpoint's error message surfaces
/// correctly, regardless of which shape that particular route uses.
/// </summary>
public static class BackendErrorParser
{
    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var kind = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => ApiErrorKind.Unauthorized,
            HttpStatusCode.Forbidden => ApiErrorKind.Forbidden,
            HttpStatusCode.NotFound => ApiErrorKind.NotFound,
            HttpStatusCode.Conflict => ApiErrorKind.Conflict,
            // 400 is what watchlist's UnknownSymbolError and the ValueErrors
            // raised by analyze-stock/top-picks-start (invalid symbol/category/
            // insufficient data) come back as - the same "fix your input and
            // retry" bucket as FastAPI's own 422 request-validation errors.
            HttpStatusCode.BadRequest => ApiErrorKind.ValidationFailed,
            HttpStatusCode.UnprocessableEntity => ApiErrorKind.ValidationFailed,
            HttpStatusCode.TooManyRequests => ApiErrorKind.TooManyRequests,
            >= HttpStatusCode.InternalServerError => ApiErrorKind.ServerError,
            _ => ApiErrorKind.Unknown,
        };

        var message = await ExtractMessageAsync(response, ct);
        return new ApiException(kind, message, (int)response.StatusCode);
    }

    private static async Task<string> ExtractMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                if (detail.ValueKind == JsonValueKind.String)
                    return detail.GetString() ?? DefaultMessageFor(response.StatusCode);

                if (detail.ValueKind == JsonValueKind.Array)
                {
                    var messages = new List<string>();
                    foreach (var item in detail.EnumerateArray())
                    {
                        if (item.TryGetProperty("msg", out var msg) && msg.ValueKind == JsonValueKind.String)
                            messages.Add(msg.GetString()!);
                    }
                    return messages.Count > 0 ? string.Join(" ", messages) : DefaultMessageFor(response.StatusCode);
                }
            }

            // api/main.py's centralized ValueError/KeyError/Exception handlers
            // (used by analyze-stock, top-picks, performance, intelligence)
            // return {"success": false, "error": "<category>", "details":
            // "<specific message>"} instead - prefer the specific "details"
            // string, since "error" is just a generic category label.
            if (doc.RootElement.TryGetProperty("details", out var details) &&
                details.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(details.GetString()))
            {
                return details.GetString()!;
            }

            if (doc.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                return error.GetString() ?? DefaultMessageFor(response.StatusCode);

            return DefaultMessageFor(response.StatusCode);
        }
        catch (JsonException)
        {
            // Malformed/non-JSON body (e.g. a plain-text 5xx from an
            // intermediary proxy) - fall back rather than throw here.
            return DefaultMessageFor(response.StatusCode);
        }
    }

    private static string DefaultMessageFor(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
        HttpStatusCode.Forbidden => "You don't have permission to do that.",
        HttpStatusCode.NotFound => "The requested item was not found.",
        HttpStatusCode.Conflict => "That already exists.",
        HttpStatusCode.UnprocessableEntity => "Please check the information you entered.",
        HttpStatusCode.TooManyRequests => "Too many requests. Please wait and try again.",
        >= HttpStatusCode.InternalServerError => "The server encountered a problem. Please try again later.",
        _ => "Something went wrong. Please try again.",
    };
}
