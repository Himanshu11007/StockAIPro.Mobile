using System.Text.RegularExpressions;
using StockAIPro.Mobile.Models.Common;
using StockAIPro.Mobile.Models.Product;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Tests;

/// <summary>StockLens branding regression tests (backend docs/BRANDING.md):
/// display name, icon/splash configuration, user-facing text and a source
/// scan that fails if the legacy brand reappears in anything a user sees.
/// Technical identifiers (namespaces StockAIPro.*, the StockAiScore
/// property, stockaipro:// scheme, stockai_alerts channel id, application
/// id) are intentionally kept and are not matched by the pattern.</summary>
public partial class BrandingTests
{
    [GeneratedRegex(@"StockAI Pro|\bStockAI\b(?![A-Za-z])|\bStock AI\b|Hemanshu (Stock )?Filter")]
    private static partial Regex Legacy();

    private static DirectoryInfo RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "StockAIPro.Mobile.slnx")))
            d = d.Parent;
        return d ?? throw new InvalidOperationException("repository root not found");
    }

    [Fact]
    public void App_display_name_icon_and_splash_are_StockLens()
    {
        var csproj = File.ReadAllText(Path.Combine(RepoRoot().FullName, "StockAIPro.Mobile", "StockAIPro.Mobile.csproj"));
        Assert.Contains("<ApplicationTitle>StockLens</ApplicationTitle>", csproj);
        Assert.Contains(@"ForegroundFile=""Resources\AppIcon\stocklens_icon.png""", csproj);
        Assert.Contains(@"Resources\Splash\stocklens_splash.png", csproj);
        Assert.DoesNotContain("#512BD4", csproj);                       // template purple removed
        // Technical identity deliberately unchanged (see docs/BRANDING.md).
        Assert.Contains("<ApplicationId>com.companyname.stockaipro.mobile</ApplicationId>", csproj);
        var app = Path.Combine(RepoRoot().FullName, "StockAIPro.Mobile");
        Assert.True(File.Exists(Path.Combine(app, "Resources", "AppIcon", "stocklens_icon.png")));
        Assert.True(File.Exists(Path.Combine(app, "Platforms", "Android", "Resources", "drawable-xxhdpi", "ic_stat_stocklens.png")));
        Assert.False(File.Exists(Path.Combine(app, "Resources", "AppIcon", "appiconfg.svg")));
        Assert.Contains("Title = \"StockLens\"", File.ReadAllText(Path.Combine(app, "App.xaml.cs")));
    }

    [Fact]
    public void User_facing_texts_use_the_brand()
    {
        Assert.StartsWith("StockLens provides research and analysis", AppConfigService.FallbackDisclaimer);
        Assert.Contains("StockLens server", ApiException.NetworkUnavailable(new HttpRequestException()).Message);
        var cfg = System.Text.Json.JsonSerializer.Deserialize<ApiEnvelope<AppConfig>>(File.ReadAllText("Fixtures/app_config.json"))!.Data;
        Assert.DoesNotMatch(Legacy(), cfg.Disclaimer!);
    }

    [Fact]
    public void No_legacy_brand_in_user_visible_sources()
    {
        var root = RepoRoot().FullName;
        var offenders = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}") &&
                        !f.EndsWith("BrandingTests.cs"))
            .Where(f => f.EndsWith(".razor") || f.EndsWith(".cs") || f.EndsWith(".xaml") || f.EndsWith(".html") ||
                        f.EndsWith(".plist") || f.EndsWith(".json") || f.EndsWith(".css") || f.EndsWith(".md") ||
                        f.EndsWith(".xml"))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x => Legacy().IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(root, x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0, "legacy brand found:\n" + string.Join("\n", offenders.Take(30)));
    }
}
