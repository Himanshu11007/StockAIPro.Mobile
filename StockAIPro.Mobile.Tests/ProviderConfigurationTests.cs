using StockAIPro.Mobile.Services.Authentication;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Tests;

/// <summary>Covers the Phase 8.1 provider-configuration contract: unless a
/// real Google OAuth client id has been supplied, the app must fail closed
/// rather than attempt a sign-in with an empty/placeholder audience - see
/// GoogleAuthConfiguration's own doc comment and
/// Platforms/Android/GoogleSignInService.cs:SignInAsync's IsConfigured
/// check.</summary>
[Collection("GoogleAuthConfiguration")]
public class ProviderConfigurationTests
{
    [Fact]
    public void GoogleAuthConfiguration_is_not_configured_without_a_real_client_id()
    {
        // ServerClientId ships empty - a real value must be supplied before
        // the app is built for production (see docs/AUTHENTICATION.md).
        Assert.False(GoogleAuthConfiguration.IsConfigured);
    }

    [Fact]
    public void GoogleSignInNotConfiguredException_has_a_user_safe_message()
    {
        var ex = new GoogleSignInNotConfiguredException();
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.DoesNotContain("Exception", ex.Message); // no raw type-name leakage
    }

    [Fact]
    public void AppleSignInNotConfiguredException_has_a_user_safe_message()
    {
        var ex = new AppleSignInNotConfiguredException();
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.DoesNotContain("Exception", ex.Message);
    }

    [Fact]
    public void GoogleSignInFailedException_preserves_inner_exception_for_diagnostics()
    {
        var inner = new InvalidOperationException("raw provider detail - never shown to the user");
        var ex = new GoogleSignInFailedException("Google sign-in is currently unavailable. Please try again.", inner);

        Assert.Equal("Google sign-in is currently unavailable. Please try again.", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AppleSignInFailedException_preserves_inner_exception_for_diagnostics()
    {
        var inner = new InvalidOperationException("raw provider detail - never shown to the user");
        var ex = new AppleSignInFailedException("Apple sign-in is currently unavailable. Please try again.", inner);

        Assert.Equal("Apple sign-in is currently unavailable. Please try again.", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}

[Collection("GoogleAuthConfiguration")]
public class GoogleClientIdConfigurationTests
{
    [Fact]
    public void Initialize_accepts_only_google_oauth_client_ids()
    {
        try
        {
            GoogleAuthConfiguration.Initialize("1234-abc.apps.googleusercontent.com");
            Assert.True(GoogleAuthConfiguration.IsConfigured);
            Assert.Equal("1234-abc.apps.googleusercontent.com", GoogleAuthConfiguration.ServerClientId);
            GoogleAuthConfiguration.Initialize("not-a-client-id");
            Assert.False(GoogleAuthConfiguration.IsConfigured);
            GoogleAuthConfiguration.Initialize(null);
            Assert.False(GoogleAuthConfiguration.IsConfigured);
        }
        finally
        {
            GoogleAuthConfiguration.Initialize(null);
        }
    }
}
