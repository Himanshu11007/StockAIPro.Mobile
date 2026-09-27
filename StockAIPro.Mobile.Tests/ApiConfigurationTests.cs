using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Tests;

public class ApiConfigurationTests
{
    [Fact]
    public void BaseUrl_resolves_via_runtime_OS_check_not_a_stale_compile_time_symbol()
    {
        // Regression test for a real bug caught during Phase 7A's Android
        // emulator run: ApiConfiguration originally used "#if ANDROID",
        // which is only ever defined when THIS FILE's own assembly is
        // compiled for the android TFM. It lives in
        // StockAIPro.Mobile.Core (plain net10.0, for testability - see
        // that project's csproj comment), so "#if ANDROID" was always
        // false there even when the Mobile app was actually running on
        // Android - the client silently pointed at 127.0.0.1 (the
        // emulator itself) instead of 10.0.2.2 (the host), and every
        // request failed as "network unavailable". Fixed to use
        // OperatingSystem.IsAndroid() (a runtime check) instead.
        //
        // This test runs on the desktop test host, so it can only assert
        // the non-Android branch - there is no way to exercise the
        // Android branch outside an actual Android runtime (the same
        // reason this whole project can't run its tests as an
        // Android-targeted assembly - see Tests.csproj's own comment).
        Assert.False(OperatingSystem.IsAndroid());
        Assert.Contains("127.0.0.1", ApiConfiguration.BaseUrl);
        Assert.DoesNotContain("10.0.2.2", ApiConfiguration.BaseUrl);
    }
}
