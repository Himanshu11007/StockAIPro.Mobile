namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// Coarse-grained classification the UI can switch on to decide what to
/// show - never expose a raw stack trace or exception message to the user.
/// </summary>
public enum ApiErrorKind
{
    /// <summary>No connection, DNS failure, timeout, or the backend refused
    /// the connection outright - distinct from an authenticated-but-denied
    /// response (Unauthorized/Forbidden below).</summary>
    NetworkUnavailable,
    Unauthorized,      // 401
    Forbidden,         // 403
    NotFound,          // 404
    Conflict,          // 409 - e.g. duplicate email on register
    ValidationFailed,  // 422
    TooManyRequests,   // 429
    ServerError,       // 5xx
    Unknown,
}

/// <summary>
/// Thrown by Services/Api clients for any non-success response or transport
/// failure. Message is safe to show directly in the UI (already derived
/// from the backend's {"detail": ...} body, or a generic description for
/// transport-level failures) - never includes a token or raw exception text.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiErrorKind Kind { get; }
    public int? StatusCode { get; }

    public ApiException(ApiErrorKind kind, string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        StatusCode = statusCode;
    }

    public static ApiException NetworkUnavailable(Exception inner) =>
        new(ApiErrorKind.NetworkUnavailable,
            "Could not reach the StockLens server. Check your connection and try again.",
            null, inner);
}
