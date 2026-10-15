using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;

namespace MusicPlayer.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string LanguagePreferenceKey = "language";
    private static readonly ResourceManager Resources = new(
        "MusicPlayer.Resources.Localization.AppResources",
        typeof(LocalizationService).Assembly);

    private CultureInfo _currentCulture;

    public LocalizationService()
    {
        Instance = this;
        _currentCulture = ResolveCulture(Preferences.Default.Get(LanguagePreferenceKey, "ru-RU"));
        ApplyCulture(_currentCulture);
    }

    public static LocalizationService Instance { get; private set; } = null!;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LanguageChanged;

    public CultureInfo CurrentCulture => _currentCulture;

    public string CurrentLanguage => _currentCulture.Name;

    public string this[string key] => Resources.GetString(key, _currentCulture) ?? key;

    public void SetLanguage(string languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        var culture = ResolveCulture(languageCode);
        if (culture.Name == _currentCulture.Name)
        {
            return;
        }

        _currentCulture = culture;
        Preferences.Default.Set(LanguagePreferenceKey, culture.Name);
        ApplyCulture(culture);
        OnPropertyChanged(nameof(CurrentCulture));
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged("Item[]");
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static CultureInfo ResolveCulture(string languageCode) =>
        languageCode.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("en-US")
            : CultureInfo.GetCultureInfo("ru-RU");

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}