using StockAIPro.Mobile.Models.Product;

namespace StockAIPro.Mobile.Tests;

public class ProductFormatTests
{
    [Fact]
    public void Missing_values_render_as_dash_never_zero()
    {
        Assert.Equal("-", ProductFormat.Score(null));
        Assert.Equal("-", ProductFormat.Percent(null));
        Assert.Equal("-", ProductFormat.Price(null));
        Assert.Equal("-", ProductFormat.Date(null));
        Assert.Equal("-", ProductFormat.Date(" "));
    }

    [Fact]
    public void Values_are_formatted_invariantly()
    {
        Assert.Equal("77.0", ProductFormat.Score(77));
        Assert.Equal("95.3%", ProductFormat.Percent(0.953));
        Assert.Equal("1,167.70", ProductFormat.Price(1167.7));
        Assert.Equal("2026-10-03", ProductFormat.Date("2026-10-03T09:15:53.834848+00:00"));
        Assert.Equal("Q3", ProductFormat.Date("Q3"));
    }

    [Theory]
    [InlineData("PASS", "sai-badge-positive", "Pass")]
    [InlineData("FAIL", "sai-badge-negative", "Fail")]
    [InlineData("WARNING", "sai-badge-warning", "Warning")]
    [InlineData("NOT_AVAILABLE", "sai-badge-neutral", "Not available")]
    public void Fqvf_status_presentation(string status, string css, string label)
    {
        Assert.Equal(css, ProductFormat.StatusCss(status));
        Assert.Equal(label, ProductFormat.StatusLabel(status));
    }
}
