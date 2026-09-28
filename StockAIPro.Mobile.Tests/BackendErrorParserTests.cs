using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Services.Api;

namespace StockAIPro.Mobile.Tests;

public class BackendErrorParserTests
{
    [Fact]
    public async Task FastAPI_HTTPException_shape_uses_the_detail_string()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = JsonContent.Create(new { detail = "Stock not found" }),
        };

        var ex = await BackendErrorParser.FromResponseAsync(response, default);

        Assert.Equal(ApiErrorKind.NotFound, ex.Kind);
        Assert.Equal("Stock not found", ex.Message);
    }

    [Fact]
    public async Task Centralized_handler_shape_prefers_details_over_error()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new { success = false, error = "Invalid request", details = "symbol must not be empty" }),
        };

        var ex = await BackendErrorParser.FromResponseAsync(response, default);

        Assert.Equal(ApiErrorKind.ValidationFailed, ex.Kind);
        Assert.Equal("symbol must not be empty", ex.Message);
    }

    [Fact]
    public async Task Centralized_handler_shape_falls_back_to_error_when_details_is_missing()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = JsonContent.Create(new { success = false, error = "Internal server error" }),
        };

        var ex = await BackendErrorParser.FromResponseAsync(response, default);

        Assert.Equal(ApiErrorKind.ServerError, ex.Kind);
        Assert.Equal("Internal server error", ex.Message);
    }

    [Fact]
    public async Task Malformed_body_falls_back_to_a_default_message_for_the_status_code()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("<html>not json</html>"),
        };

        var ex = await BackendErrorParser.FromResponseAsync(response, default);

        Assert.Equal(ApiErrorKind.ServerError, ex.Kind);
        Assert.Equal("The server encountered a problem. Please try again later.", ex.Message);
    }
}
