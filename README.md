# StockAIPro.Mobile

.NET MAUI Blazor Hybrid app (Android, iOS) for StockAI Pro. The app is a
thin client: every score, FQVF check, ranking, explanation, feature flag and
disclaimer comes from the backend
([stock-prediction-app](https://github.com/Himanshu11007/stock-prediction-app)).

| Project | Purpose |
|---|---|
| `StockAIPro.Mobile.Core` | net10.0 library: models, API clients (`ProductApiClient`, auth, stocks, watchlist, …), authentication/session services, API configuration, app-config service. Unit tested. |
| `StockAIPro.Mobile` | MAUI Blazor UI, platform services (SecureStorage token/PIN stores, Google/Apple sign-in) |
| `StockAIPro.Mobile.Tests` | xUnit tests for Core |

## Screens

Home · Analyse (stock search) · Stock Analysis (StockAI Score, components,
positive factors, risks, 18 Fundamental Quality & Value Framework checks,
market data, informational ML signal, data freshness, on-demand analysis) ·
Top Investment Candidates · Watchlist · Performance · AI Intelligence ·
Account · Login / Register / OTP / PIN.

## Build and run

```bash
dotnet test StockAIPro.Mobile.Tests/StockAIPro.Mobile.Tests.csproj
dotnet build StockAIPro.Mobile/StockAIPro.Mobile.csproj -f net10.0-android -t:Run          # emulator/device (dev)
dotnet build StockAIPro.Mobile/StockAIPro.Mobile.csproj -f net10.0-android -c Debug \
    -p:AndroidPackageFormat=apk -p:EmbedAssembliesIntoApk=true                             # standalone APK
dotnet publish StockAIPro.Mobile/StockAIPro.Mobile.csproj -f net10.0-android -c Release \
    -p:StockAIProApiBaseUrl=https://api.your-domain                                       # release
```

API URL: Debug builds use the local backend (`http://10.0.2.2:8000` on the
Android emulator, `http://127.0.0.1:8000` elsewhere). Physical devices and
production set `-p:StockAIProApiBaseUrl=…`; Release builds require it and
require HTTPS. Full instructions: `docs/MOBILE_SETUP.md` in the backend
repository. iOS packaging requires a Mac with Xcode.
