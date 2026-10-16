using Microsoft.Maui.Storage;

namespace MusicPlayer.Services;

public sealed class ThemeService : IThemeService
{
    private const string ThemePreferenceKey = "theme";

    private bool _subscribed;

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

        Subscribe(application);

        application.UserAppTheme = CurrentMode switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        ApplyPalette(application, ResolveEffectiveTheme(application));
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

    private AppTheme ResolveEffectiveTheme(Application application) => CurrentMode switch
    {
        ThemeMode.Light => AppTheme.Light,
        ThemeMode.Dark => AppTheme.Dark,
        _ => application.RequestedTheme == AppTheme.Dark ? AppTheme.Dark : AppTheme.Light
    };

    private static void ApplyPalette(Application application, AppTheme theme)
    {
        var palette = theme == AppTheme.Dark ? ThemePalette.Dark : ThemePalette.Light;
        var resources = application.Resources;

        // Assigning directly to the application resources raises a change
        // notification for every key, so DynamicResource usages update instantly.
        foreach (var entry in palette)
        {
            resources[entry.Key] = entry.Value;
        }
    }

    private void Subscribe(Application application)
    {
        if (_subscribed)
        {
            return;
        }

        application.RequestedThemeChanged += OnRequestedThemeChanged;
        _subscribed = true;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs eventArgs)
    {
        if (CurrentMode == ThemeMode.System && Application.Current is { } application)
        {
            ApplyPalette(application, eventArgs.RequestedTheme == AppTheme.Dark ? AppTheme.Dark : AppTheme.Light);
        }
    }
}
