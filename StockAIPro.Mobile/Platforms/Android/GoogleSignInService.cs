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
            ?? throw new GoogleSignInFailedException("Google sign-in is currently unavailable. Please try again.");

        try
        {
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

            var response = await tcs.Task;

            // Only a Google ID-token credential is acceptable; CreateFrom
            // throws for anything else (e.g. a password credential).
            var data = response.Credential?.Data
                ?? throw new GoogleSignInFailedException("Google sign-in returned no account. Please try again.");
            var idToken = GoogleIdTokenCredential.CreateFrom(data).IdToken;
            if (string.IsNullOrWhiteSpace(idToken))
                throw new GoogleSignInFailedException("Google sign-in returned no account. Please try again.");
            return idToken;
        }
        catch (System.OperationCanceledException)
        {
            return null; // user cancelled the native sign-in flow
        }
        catch (GoogleSignInFailedException)
        {
            throw;
        }
        catch (CredentialManagerErrorException ex) when (ex.JavaClassName.Contains("NoCredential", StringComparison.Ordinal))
        {
            // NoCredentialException: no Google account on the device, or -
            // the usual cause during setup - this build's package name /
            // signing-certificate SHA-1 is not registered as an Android OAuth
            // client in the same Google Cloud project as ServerClientId (see
            // the backend's docs/AUTHENTICATION.md "Android configuration").
            System.Diagnostics.Debug.WriteLine($"Google sign-in: {ex.JavaClassName}");
            throw new GoogleSignInFailedException(
                "No Google account is available for sign-in. Add a Google account to this device and try again.", ex);
        }
        catch (Exception ex)
        {
            // Never let a raw Java/binding exception escape to the UI (the
            // Razor page only handles the IGoogleSignInService exceptions).
            System.Diagnostics.Debug.WriteLine($"Google sign-in failed: {ex.GetType().Name}");
            throw new GoogleSignInFailedException("Google sign-in is currently unavailable. Please try again.", ex);
        }
    }

    /// <summary>Carries the Credential Manager error's Java class name
    /// (e.g. androidx.credentials.exceptions.NoCredentialException) - the
    /// only reliable discriminator this binding exposes, see OnError.</summary>
    private sealed class CredentialManagerErrorException(string javaClassName)
        : Exception("Credential Manager error: " + javaClassName)
    {
        public string JavaClassName { get; } = javaClassName;
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

            tcs.TrySetException(new CredentialManagerErrorException(javaClassName));
        }
    }
}
