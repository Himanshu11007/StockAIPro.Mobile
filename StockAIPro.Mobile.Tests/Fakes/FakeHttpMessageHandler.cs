using System.Net;

namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>
/// Records every request it sees and returns responses from a queue (or a
/// single repeated response if only one was configured). Used to verify
/// exactly what AuthApiClient sends, and to simulate backend responses
/// (including a 401-then-200 sequence for refresh-flow tests) without any
/// real network call.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string?> RequestBodies { get; } = [];

    public void Enqueue(HttpStatusCode statusCode, HttpContent? content = null)
    {
        _responses.Enqueue(_ => new HttpResponseMessage(statusCode) { Content = content });
    }

    public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        _responses.Enqueue(factory);
    }

    /// <summary>Throws a transport-level failure (simulating "backend unreachable")
    /// for the next request instead of returning a response.</summary>
    public void EnqueueNetworkFailure()
    {
        _responses.Enqueue(_ => throw new HttpRequestException("simulated network failure"));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_responses.Count == 0)
            throw new InvalidOperationException("FakeHttpMessageHandler: no response queued for " + request.RequestUri);

        return _responses.Dequeue()(request);
    }
}
