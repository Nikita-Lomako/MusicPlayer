namespace MusicPlayer.Services;

public interface IThemeService
{
    ThemeMode CurrentMode { get; }

    void ApplyCurrentTheme();

    void SetTheme(ThemeMode mode);
}