using StockAIPro.Mobile.Models.Intelligence;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

public sealed class IntelligenceApiClient : IIntelligenceApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IntelligenceApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IntelligenceReport> GetReportAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/intelligence/report", ct), ct);

        return await BusinessApiSend.ReadDataOrThrowAsync<IntelligenceReport>(response, ct);
    }
}
