using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly IThemeService _themeService;
    private readonly IScanFolderService _scanFolderService;
    private readonly IMediaScannerService _mediaScannerService;
    private readonly IMusicLibraryService _musicLibrary;
    private bool _updatingSelection;

    public SettingsViewModel(
        ILocalizationService localizationService,
        IThemeService themeService,
        IScanFolderService scanFolderService,
        IMediaScannerService mediaScannerService,
        IMusicLibraryService musicLibrary)
    {
        _localizationService = localizationService;
        _themeService = themeService;
        _scanFolderService = scanFolderService;
        _mediaScannerService = mediaScannerService;
        _musicLibrary = musicLibrary;

        ThemeOptions = BuildThemeOptions();
        LanguageOptions = BuildLanguageOptions();

        _updatingSelection = true;
        SelectedThemeOption = ThemeOptions.First(option => option.Mode == _themeService.CurrentMode);
        SelectedLanguageOption = LanguageOptions.First(option => option.CultureName == _localizationService.CurrentLanguage);
        _updatingSelection = false;

        ScanFolders = [];
        RefreshFolders();

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private ObservableCollection<ThemeOption> themeOptions = [];

    [ObservableProperty]
    private ObservableCollection<LanguageOption> languageOptions = [];

    public ObservableCollection<string> ScanFolders { get; }

    public string AppearanceSection => _localizationService["AppearanceSection"];

    public string ThemeLabel => _localizationService["ThemeLabel"];

    public string LanguageSection => _localizationService["LanguageSection"];

    public string ScanSection => _localizationService["SettingsScanSection"];

    public string FoldersLabel => _localizationService["SettingsFoldersLabel"];

    public string AddFolderLabel => _localizationService["AddFolderButton"];

    public string RemoveFolderLabel => _localizationService["RemoveFolderLabel"];

    public string RescanLabel => _localizationService["RescanButton"];

    public string PickFileLabel => _localizationService["PickFileButton"];

    public string NoFoldersHint => _localizationService["NoFoldersSelected"];

    public string RemovedFoldersSection => _localizationService["RemovedFoldersSection"];

    public string RemovedFoldersHint => _localizationService["RemovedFoldersHint"];

    public string ClearRemovedLabel => _localizationService["ClearRemovedTracksButton"];

    public bool HasFolders => ScanFolders.Count > 0;

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasRemovedFolders => !string.IsNullOrWhiteSpace(RemovedFoldersText);

    [ObservableProperty]
    private ThemeOption? selectedThemeOption;

    [ObservableProperty]
    private LanguageOption? selectedLanguageOption;

    [ObservableProperty]
    private bool isScanning;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string? removedFoldersText;

    partial void OnStatusMessageChanged(string? value) => OnPropertyChanged(nameof(HasStatus));

    partial void OnRemovedFoldersTextChanged(string? value) => OnPropertyChanged(nameof(HasRemovedFolders));

    partial void OnSelectedThemeOptionChanged(ThemeOption? value)
    {
        if (!_updatingSelection && value is not null)
        {
            _themeService.SetTheme(value.Mode);
        }
    }

    partial void OnSelectedLanguageOptionChanged(LanguageOption? value)
    {
        if (!_updatingSelection && value is not null && value.CultureName != _localizationService.CurrentLanguage)
        {
            _localizationService.SetLanguage(value.CultureName);
        }
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (result.IsSuccessful && result.Folder is { } folder)
            {
                _scanFolderService.AddFolder(folder.Path);
                RefreshFolders();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = exception.Message;
        }
    }

    [RelayCommand]
    private void RemoveFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        _scanFolderService.RemoveFolder(folder);
        RefreshFolders();
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        if (IsScanning)
        {
            return;
        }

        IsScanning = true;
        StatusMessage = _localizationService["ScanningStatus"];
        try
        {
            var folders = _scanFolderService.GetEffectiveFolders();
            var tracks = await _mediaScannerService.ScanFoldersAsync(folders);
            if (tracks.Count > 0)
            {
                await _musicLibrary.SaveTracksAsync(tracks);
            }

            StatusMessage = string.Format(_localizationService["ScanCompletedFormat"], tracks.Count);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        try
        {
            var track = await _mediaScannerService.PickAudioFileAsync();
            if (track is not null)
            {
                await _musicLibrary.SaveTracksAsync([track]);
                StatusMessage = string.Format(_localizationService["ScanCompletedFormat"], 1);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            StatusMessage = exception.Message;
        }
    }

    [RelayCommand]
    private async Task ClearRemovedTracksAsync()
    {
        var removedFolders = _scanFolderService.GetRemovedFolders();
        if (removedFolders.Count == 0)
        {
            return;
        }

        var confirmed = await Shell.Current.DisplayAlertAsync(
            _localizationService["ClearRemovedConfirmTitle"],
            string.Format(_localizationService["ClearRemovedConfirmMessage"], removedFolders.Count),
            _localizationService["ConfirmButton"],
            _localizationService["CancelButton"]);

        if (!confirmed)
        {
            return;
        }

        try
        {
            var keys = removedFolders
                .Select(_mediaScannerService.GetFolderKey)
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .ToArray();

            var removed = await _musicLibrary.RemoveTracksFromFoldersAsync(keys);
            _scanFolderService.ClearRemovedFolders();
            RefreshFolders();
            StatusMessage = string.Format(_localizationService["ClearRemovedCompletedFormat"], removed);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
            StatusMessage = exception.Message;
        }
    }

    private void RefreshFolders()
    {
        ScanFolders.Clear();
        foreach (var folder in _scanFolderService.GetSelectedFolders())
        {
            ScanFolders.Add(folder);
        }

        var removed = _scanFolderService.GetRemovedFolders();
        RemovedFoldersText = removed.Count == 0
            ? null
            : string.Join(Environment.NewLine, removed);

        OnPropertyChanged(nameof(HasFolders));
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs) =>
        Application.Current?.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), RefreshLocalizedText);

    private void RefreshLocalizedText()
    {
        // Rebuild the option collections so the pickers re-read the localized
        // labels. The MAUI Picker caches the display text of its items, so
        // mutating the option name alone does not refresh a visible dropdown.
        ThemeOptions = BuildThemeOptions();
        LanguageOptions = BuildLanguageOptions();

        _updatingSelection = true;
        SelectedThemeOption = ThemeOptions.First(option => option.Mode == _themeService.CurrentMode);
        SelectedLanguageOption = LanguageOptions.First(option => option.CultureName == _localizationService.CurrentLanguage);
        _updatingSelection = false;

        OnPropertyChanged(nameof(AppearanceSection));
        OnPropertyChanged(nameof(ThemeLabel));
        OnPropertyChanged(nameof(LanguageSection));
        OnPropertyChanged(nameof(ScanSection));
        OnPropertyChanged(nameof(FoldersLabel));
        OnPropertyChanged(nameof(AddFolderLabel));
        OnPropertyChanged(nameof(RemoveFolderLabel));
        OnPropertyChanged(nameof(RescanLabel));
        OnPropertyChanged(nameof(PickFileLabel));
        OnPropertyChanged(nameof(NoFoldersHint));
        OnPropertyChanged(nameof(RemovedFoldersSection));
        OnPropertyChanged(nameof(RemovedFoldersHint));
        OnPropertyChanged(nameof(ClearRemovedLabel));
    }

    private ObservableCollection<ThemeOption> BuildThemeOptions() =>
    [
        new ThemeOption(ThemeMode.System, _localizationService["ThemeSystem"]),
        new ThemeOption(ThemeMode.Light, _localizationService["ThemeLight"]),
        new ThemeOption(ThemeMode.Dark, _localizationService["ThemeDark"])
    ];

    private ObservableCollection<LanguageOption> BuildLanguageOptions() =>
    [
        new LanguageOption("ru-RU", _localizationService["LanguageRussian"]),
        new LanguageOption("en-US", _localizationService["LanguageEnglish"])
    ];
}

public sealed partial class ThemeOption : ObservableObject
{
    public ThemeOption(ThemeMode mode, string name)
    {
        Mode = mode;
        Name = name;
    }

    public ThemeMode Mode { get; }

    [ObservableProperty]
    private string name;
}

public sealed partial class LanguageOption : ObservableObject
{
    public LanguageOption(string cultureName, string name)
    {
        CultureName = cultureName;
        Name = name;
    }

    public string CultureName { get; }

    [ObservableProperty]
    private string name;
}
