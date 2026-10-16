using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MusicPlayer.Models;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly IMediaScannerService _mediaScanner;
    private readonly IMusicLibraryService _musicLibrary;
    private readonly IScanFolderService _scanFolderService;
    private readonly IAudioService _audioService;
    private readonly ILocalizationService _localizationService;
    private bool _hasInitialized;

    public LibraryViewModel(
        IMediaScannerService mediaScanner,
        IMusicLibraryService musicLibrary,
        IScanFolderService scanFolderService,
        IAudioService audioService,
        ILocalizationService localizationService)
    {
        _mediaScanner = mediaScanner;
        _musicLibrary = musicLibrary;
        _scanFolderService = scanFolderService;
        _audioService = audioService;
        _localizationService = localizationService;
        Tracks = [];
        FilteredTracks = [];
        SortOptions =
        [
            new TrackSortOption(TrackSortMode.Title, _localizationService["SortTitle"]),
            new TrackSortOption(TrackSortMode.Artist, _localizationService["SortArtist"]),
            new TrackSortOption(TrackSortMode.DateAdded, _localizationService["SortDate"]),
            new TrackSortOption(TrackSortMode.Duration, _localizationService["SortDuration"])
        ];
        SelectedSortOption = SortOptions[0];

        _audioService.PropertyChanged += OnAudioServicePropertyChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;
        _musicLibrary.TracksChanged += OnLibraryTracksChanged;
    }

    public ObservableCollection<Track> Tracks { get; }

    public ObservableCollection<Track> FilteredTracks { get; }

    public ObservableCollection<TrackSortOption> SortOptions { get; }

    public Track? CurrentTrack => _audioService.CurrentTrack;

    public bool IsPlaying => _audioService.IsPlaying;

    public bool IsMiniPlayerVisible => CurrentTrack is not null;

    public string MiniPlayerTitle => CurrentTrack?.Title ?? _localizationService["NoTrackPlaying"];

    public string MiniPlayerArtist => CurrentTrack?.Artist ?? string.Empty;

    public string PlayPauseIcon => _audioService.IsPlaying ? "pause.png" : "play.png";

    public string SearchPlaceholder => _localizationService["SearchPlaceholder"];

    public string LibraryTitle => _localizationService["LibraryTitle"];

    public string SortLabel => _localizationService["SortLabel"];

    public string RescanLabel => _localizationService["RescanButton"];

    public string AddFileLabel => _localizationService["AddFileButton"];

    public string EmptyMessage => Tracks.Count == 0
        ? _localizationService["EmptyLibrary"]
        : _localizationService["NoSearchResults"];

    public string PlayerTitle => _localizationService["PlayerTitle"];

    public string PlayLabel => _localizationService["PlayButton"];

    public string PauseLabel => _localizationService["PauseButton"];

    public string NextLabel => _localizationService["NextButton"];

    public string PreviousLabel => _localizationService["PreviousButton"];

    public string ScanErrorLabel => _localizationService["ScanError"];

    public string TrackInfoLabel => _localizationService["TrackInfoLabel"];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private TrackSortOption? selectedSortOption;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedSortOptionChanged(TrackSortOption? value) => ApplyFilter();

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    public async Task InitializeAsync()
    {
        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        await LoadFromLibraryAsync();

        // Always rescan the folders the user already added so the library
        // reflects the file system on every launch.
        await ScanLibraryAsync();
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        _hasInitialized = true;
        await ScanLibraryAsync();
    }

    [RelayCommand]
    private async Task AddFileAsync()
    {
        ErrorMessage = null;
        try
        {
            var track = await _mediaScanner.PickAudioFileAsync();
            if (track is null)
            {
                return;
            }

            await _musicLibrary.SaveTracksAsync([track]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ErrorMessage = exception.Message;
        }
    }

    [RelayCommand]
    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        var queue = FilteredTracks.ToArray();
        var index = Array.IndexOf(queue, track);
        if (index >= 0)
        {
            await _audioService.SetQueueAsync(queue, index);
        }
    }

    [RelayCommand]
    private async Task TogglePlaybackAsync()
    {
        if (_audioService.IsPlaying)
        {
            await _audioService.PauseAsync();
        }
        else
        {
            await _audioService.PlayAsync();
        }
    }

    [RelayCommand]
    private Task NextAsync() => _audioService.NextAsync();

    [RelayCommand]
    private async Task PreviousAsync() => await _audioService.PreviousAsync();

    [RelayCommand]
    private async Task OpenPlayerAsync()
    {
        if (CurrentTrack is not null)
        {
            await Shell.Current.GoToAsync("player");
        }
    }

    [RelayCommand]
    private async Task OpenDetailsAsync(Track? track)
    {
        if (track is { Id: > 0 })
        {
            await Shell.Current.GoToAsync($"trackdetails?id={track.Id}");
        }
    }

    private async Task ScanLibraryAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var folders = _scanFolderService.GetEffectiveFolders();
            var scannedTracks = await _mediaScanner.ScanFoldersAsync(folders);
            if (scannedTracks.Count > 0)
            {
                // MusicLibraryService raises TracksChanged, which reloads the list.
                await _musicLibrary.SaveTracksAsync(scannedTracks);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadFromLibraryAsync()
    {
        var stored = await _musicLibrary.GetTracksAsync();
        Tracks.Clear();
        foreach (var track in stored)
        {
            Tracks.Add(track);
        }

        ApplyFilter();
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        var source = Tracks.Where(track => string.IsNullOrEmpty(query)
            || track.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || track.Artist.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || (track.Album?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false));

        source = SelectedSortOption?.Mode switch
        {
            TrackSortMode.Artist => source.OrderBy(track => track.Artist, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(track => track.Title, StringComparer.CurrentCultureIgnoreCase),
            TrackSortMode.DateAdded => source.OrderByDescending(track => track.AddedAtUtc),
            TrackSortMode.Duration => source.OrderByDescending(track => track.Duration),
            _ => source.OrderBy(track => track.Title, StringComparer.CurrentCultureIgnoreCase)
        };

        FilteredTracks.Clear();
        foreach (var track in source)
        {
            FilteredTracks.Add(track);
        }

        OnPropertyChanged(nameof(EmptyMessage));
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs)
    {
        SortOptions[0].Name = _localizationService["SortTitle"];
        SortOptions[1].Name = _localizationService["SortArtist"];
        SortOptions[2].Name = _localizationService["SortDate"];
        SortOptions[3].Name = _localizationService["SortDuration"];
        OnPropertyChanged(nameof(MiniPlayerTitle));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(LibraryTitle));
        OnPropertyChanged(nameof(SortLabel));
        OnPropertyChanged(nameof(RescanLabel));
        OnPropertyChanged(nameof(AddFileLabel));
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(PlayerTitle));
        OnPropertyChanged(nameof(PlayLabel));
        OnPropertyChanged(nameof(PauseLabel));
        OnPropertyChanged(nameof(NextLabel));
        OnPropertyChanged(nameof(PreviousLabel));
        OnPropertyChanged(nameof(ScanErrorLabel));
        OnPropertyChanged(nameof(TrackInfoLabel));
    }

    private void OnLibraryTracksChanged(object? sender, EventArgs eventArgs) =>
        MainThread.BeginInvokeOnMainThread(() => _ = LoadFromLibraryAsync());

    private void OnAudioServicePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(IAudioService.CurrentTrack)
            or nameof(IAudioService.IsPlaying))
        {
            OnPropertyChanged(nameof(CurrentTrack));
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(IsMiniPlayerVisible));
            OnPropertyChanged(nameof(MiniPlayerTitle));
            OnPropertyChanged(nameof(MiniPlayerArtist));
            OnPropertyChanged(nameof(PlayPauseIcon));
        }
    }
}

public enum TrackSortMode
{
    Title,
    Artist,
    DateAdded,
    Duration
}

public sealed partial class TrackSortOption : ObservableObject
{
    public TrackSortOption(TrackSortMode mode, string name)
    {
        Mode = mode;
        Name = name;
    }

    public TrackSortMode Mode { get; }

    [ObservableProperty]
    private string name;
}
