using System.Net;
using System.Text.Json;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// Turns a failed HttpResponseMessage into an ApiException.
///
/// Verified directly against the running backend (see api/main.py +
/// api/routes/auth.py): errors raised as HTTPException come back as
/// {"detail": "&lt;string&gt;"}; FastAPI's own 422 validation errors come back
/// as {"detail": [{"loc": [...], "msg": "...", "type": "..."}, ...]}. This
/// is FastAPI's default shape, NOT the {"success": false, "error": ...}
/// envelope some other endpoints use - the auth routes never wrap errors
/// that way.
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

            if (!doc.RootElement.TryGetProperty("detail", out var detail))
                return DefaultMessageFor(response.StatusCode);

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
