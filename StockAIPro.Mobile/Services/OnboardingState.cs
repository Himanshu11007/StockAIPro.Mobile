namespace StockAIPro.Mobile.Services;

/// <summary>Whether this installation has seen the first-use introduction.</summary>
public static class OnboardingState
{
    private const string Key = "onboarding_completed_v1";

    public static bool Completed => Preferences.Default.Get(Key, false);

    public static void MarkCompleted() => Preferences.Default.Set(Key, true);
}
