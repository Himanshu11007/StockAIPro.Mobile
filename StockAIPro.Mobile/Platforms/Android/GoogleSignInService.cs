using Android.Content;
using Android.OS;
using AndroidX.Core.Content;
using AndroidX.Credentials;
using Google.Android.Libraries.Identity.GoogleId;
using Microsoft.Maui.ApplicationModel;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Real Android IGoogleSignInService, using Android's current supported
/// mechanism for Google sign-in: Credential Manager
/// (androidx.credentials + com.google.android.libraries.identity.googleid's
/// GetGoogleIdOption), NOT the deprecated GoogleSignInClient/
/// GoogleSignInOptions API, and NOT a browser-redirect OAuth flow (Google
/// blocked custom-URI-scheme OAuth redirects for Android apps in 2022 - see
/// docs/AUTHENTICATION.md "Google setup" for the citation trail that led to
/// this choice).
///
/// GoogleAuthConfiguration.ServerClientId is the OAuth 2.0 Web-application
/// client id - the SAME value the backend validates `id_token.aud` against
/// (see auth/external_identity.py:GoogleIdentityVerifier). It is public
/// client-side configuration, not a secret.
/// </summary>
public sealed class GoogleSignInService : IGoogleSignInService
{
    public async Task<string?> SignInAsync(CancellationToken ct = default)
    {
        if (!GoogleAuthConfiguration.IsConfigured)
            throw new GoogleSignInNotConfiguredException();

        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("No current Android activity to host Google Sign-In.");

        var googleIdOption = new GetGoogleIdOption.Builder()
            .SetFilterByAuthorizedAccounts(false)
            .SetServerClientId(GoogleAuthConfiguration.ServerClientId)
            .SetAutoSelectEnabled(false)
            .Build();

        var request = new GetCredentialRequest.Builder()
            .AddCredentialOption(googleIdOption)
            .Build();

        var credentialManager = CredentialManager.Create(activity);
        var cancellationSignal = new CancellationSignal();
        await using var ctRegistration = ct.Register(() => cancellationSignal.Cancel());
        var executor = ContextCompat.GetMainExecutor(activity);

        var tcs = new TaskCompletionSource<GetCredentialResponse>();
        credentialManager.GetCredentialAsync(
            activity, request, cancellationSignal, executor, new CredentialCallback(tcs));

        GetCredentialResponse response;
        try
        {
            response = await tcs.Task;
        }
        catch (System.OperationCanceledException)
        {
            return null; // user cancelled the native sign-in flow
        }
        catch (InvalidOperationException ex)
        {
            throw new GoogleSignInFailedException("Google sign-in is currently unavailable. Please try again.", ex);
        }

        var credential = response.Credential;
        if (credential?.Data is null)
            throw new InvalidOperationException("Google Sign-In returned no credential data.");

        var googleIdTokenCredential = GoogleIdTokenCredential.CreateFrom(credential.Data);
        return googleIdTokenCredential.IdToken;
    }

    private sealed class CredentialCallback(TaskCompletionSource<GetCredentialResponse> tcs)
        : Java.Lang.Object, ICredentialManagerCallback
    {
        public void OnResult(Java.Lang.Object? result)
        {
            if (result is GetCredentialResponse response)
                tcs.TrySetResult(response);
            else
                tcs.TrySetException(new InvalidOperationException("Unexpected Google Sign-In result type."));
        }

        public void OnError(Java.Lang.Object? error)
        {
            // GetCredentialException's concrete subclasses aren't bound with
            // a C#-visible relationship to Java.Lang.Object/Throwable in
            // this binding, so inspect the underlying Java class name
            // (still available directly on the Object reference) instead of
            // a C# `is`/`as` pattern against the specific exception type.
            var javaClassName = error?.Class?.Name ?? string.Empty;

            if (javaClassName.Contains("Cancellation", StringComparison.Ordinal))
            {
                tcs.TrySetCanceled();
                return;
            }

            tcs.TrySetException(new InvalidOperationException(
                "Google Sign-In failed: " + (error?.ToString() ?? "unknown error")));
        }
    }
}
