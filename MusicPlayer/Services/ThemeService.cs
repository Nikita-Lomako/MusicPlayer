using Microsoft.Maui.Storage;

namespace MusicPlayer.Services;

public sealed class ThemeService : IThemeService
{
    private const string ThemePreferenceKey = "theme";

    public ThemeService()
    {
        var savedTheme = Preferences.Default.Get(ThemePreferenceKey, nameof(ThemeMode.System));
        CurrentMode = Enum.TryParse<ThemeMode>(savedTheme, ignoreCase: true, out var mode)
            ? mode
            : ThemeMode.System;
    }

    public ThemeMode CurrentMode { get; private set; }

    public void ApplyCurrentTheme()
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        application.UserAppTheme = CurrentMode switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    public void SetTheme(ThemeMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        CurrentMode = mode;
        Preferences.Default.Set(ThemePreferenceKey, mode.ToString());
        ApplyCurrentTheme();
    }
}