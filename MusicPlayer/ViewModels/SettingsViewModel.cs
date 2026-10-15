using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly IThemeService _themeService;

    public SettingsViewModel(ILocalizationService localizationService, IThemeService themeService)
    {
        _localizationService = localizationService;
        _themeService = themeService;
        ThemeOptions = [];
        LanguageOptions = [];

        RefreshOptions(_themeService.CurrentMode, _localizationService.CurrentLanguage);
        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public ObservableCollection<ThemeOption> ThemeOptions { get; }

    public ObservableCollection<LanguageOption> LanguageOptions { get; }

    public string AppearanceSection => _localizationService["AppearanceSection"];

    public string ThemeLabel => _localizationService["ThemeLabel"];

    public string LanguageSection => _localizationService["LanguageSection"];

    [ObservableProperty]
    private ThemeOption? selectedThemeOption;

    [ObservableProperty]
    private LanguageOption? selectedLanguageOption;

    partial void OnSelectedThemeOptionChanged(ThemeOption? value)
    {
        if (value is not null)
        {
            _themeService.SetTheme(value.Mode);
        }
    }

    partial void OnSelectedLanguageOptionChanged(LanguageOption? value)
    {
        if (value is not null && value.CultureName != _localizationService.CurrentLanguage)
        {
            _localizationService.SetLanguage(value.CultureName);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs)
    {
        var currentTheme = SelectedThemeOption?.Mode ?? _themeService.CurrentMode;
        var currentLanguage = SelectedLanguageOption?.CultureName ?? _localizationService.CurrentLanguage;
        RefreshOptions(currentTheme, currentLanguage);
        OnPropertyChanged(nameof(AppearanceSection));
        OnPropertyChanged(nameof(ThemeLabel));
        OnPropertyChanged(nameof(LanguageSection));
    }

    private void RefreshOptions(ThemeMode selectedTheme, string selectedLanguage)
    {
        ThemeOptions.Clear();
        ThemeOptions.Add(new ThemeOption(ThemeMode.System, _localizationService["ThemeSystem"]));
        ThemeOptions.Add(new ThemeOption(ThemeMode.Light, _localizationService["ThemeLight"]));
        ThemeOptions.Add(new ThemeOption(ThemeMode.Dark, _localizationService["ThemeDark"]));
        SelectedThemeOption = ThemeOptions.First(option => option.Mode == selectedTheme);

        LanguageOptions.Clear();
        LanguageOptions.Add(new LanguageOption("ru-RU", _localizationService["LanguageRussian"]));
        LanguageOptions.Add(new LanguageOption("en-US", _localizationService["LanguageEnglish"]));
        SelectedLanguageOption = LanguageOptions.First(option => option.CultureName == selectedLanguage);
    }
}

public sealed record ThemeOption(ThemeMode Mode, string Name);

public sealed record LanguageOption(string CultureName, string Name);