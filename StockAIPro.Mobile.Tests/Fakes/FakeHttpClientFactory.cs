namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>Minimal IHttpClientFactory test double: returns a fixed
/// HttpClient per requested name (or the same one for every name, if only
/// one is configured), built on a caller-supplied handler.</summary>
public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = new();
    private readonly HttpClient? _default;

    public FakeHttpClientFactory(HttpMessageHandler handler)
    {
        _default = new HttpClient(handler) { BaseAddress = new Uri("http://backend.test") };
    }

    public FakeHttpClientFactory() { }

    public void Configure(string name, HttpMessageHandler handler)
    {
        _clients[name] = new HttpClient(handler) { BaseAddress = new Uri("http://backend.test") };
    }

    public HttpClient CreateClient(string name) =>
        _clients.TryGetValue(name, out var client) ? client
        : _default ?? throw new InvalidOperationException($"No client configured for '{name}'");
}
