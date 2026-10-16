using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MusicPlayer.Models;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public sealed partial class PlaylistDetailViewModel : ObservableObject
{
    private readonly IPlaylistService _playlistService;
    private readonly IMusicLibraryService _musicLibrary;
    private readonly IAudioService _audioService;
    private readonly ILocalizationService _localizationService;

    private readonly HashSet<int> _membership = [];
    private readonly List<LibraryTrackItem> _allLibraryItems = [];
    private int _playlistId;
    private bool _isAllTracks;

    public PlaylistDetailViewModel(
        IPlaylistService playlistService,
        IMusicLibraryService musicLibrary,
        IAudioService audioService,
        ILocalizationService localizationService)
    {
        _playlistService = playlistService;
        _musicLibrary = musicLibrary;
        _audioService = audioService;
        _localizationService = localizationService;
        _localizationService.LanguageChanged += OnLanguageChanged;
        _musicLibrary.TracksChanged += OnLibraryTracksChanged;
    }

    public ObservableCollection<Track> Tracks { get; } = [];

    public ObservableCollection<LibraryTrackItem> LibraryTracks { get; } = [];

    public bool HasTracks => Tracks.Count > 0;

    public bool IsEmpty => Tracks.Count == 0;

    public string PlaylistName { get; private set; } = string.Empty;

    public bool IsSystemPlaylist { get; private set; }

    public string EmptyMessage => _localizationService["EmptyPlaylistTracks"];

    public string AddTracksLabel => _localizationService["AddTracksButton"];

    public string DoneLabel => _localizationService["HideTracksButton"];

    public string PlayAllLabel => _localizationService["PlayAllButton"];

    public string RemoveLabel => _localizationService["RemoveTrackLabel"];

    public string AddLabel => _localizationService["AddTrackLabel"];

    public string SearchPlaceholder => _localizationService["SearchTracksPlaceholder"];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isLibraryPickerVisible;

    [ObservableProperty]
    private string searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplySearch();

    public async Task LoadAsync(int playlistId)
    {
        _playlistId = playlistId;
        IsBusy = true;
        try
        {
            var playlist = await _playlistService.GetPlaylistAsync(playlistId);
            IsSystemPlaylist = playlist?.IsSystemPlaylist ?? false;
            _isAllTracks = playlist?.IsAllTracks ?? false;
            PlaylistName = _isAllTracks
                ? _localizationService["AllTracksPlaylist"]
                : playlist?.Name ?? _localizationService["PlaylistDetailsTitle"];
            OnPropertyChanged(nameof(PlaylistName));
            OnPropertyChanged(nameof(IsSystemPlaylist));

            await ReloadTracksAsync();
            await ReloadLibraryAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_playlistId > 0)
        {
            await LoadAsync(_playlistId);
        }
    }

    [RelayCommand]
    private async Task PlayAllAsync()
    {
        var queue = Tracks.ToArray();
        if (queue.Length > 0)
        {
            await _audioService.SetQueueAsync(queue, 0);
        }
    }

    [RelayCommand]
    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        var queue = Tracks.ToArray();
        var index = Array.IndexOf(queue, track);
        if (index >= 0)
        {
            await _audioService.SetQueueAsync(queue, index);
        }
    }

    [RelayCommand]
    private async Task RemoveTrackAsync(Track? track)
    {
        if (track is null || IsSystemPlaylist)
        {
            return;
        }

        await _playlistService.RemoveTrackAsync(_playlistId, track.Id);
        _membership.Remove(track.Id);

        var libraryItem = _allLibraryItems.FirstOrDefault(item => item.Track.Id == track.Id);
        libraryItem?.Refresh(_membership.Contains(track.Id));

        await ReloadTracksAsync();
    }

    [RelayCommand]
    private void ToggleLibraryPicker() => IsLibraryPickerVisible = !IsLibraryPickerVisible;

    [RelayCommand]
    private async Task ToggleLibraryTrackAsync(LibraryTrackItem? item)
    {
        if (item is null || IsSystemPlaylist)
        {
            return;
        }

        if (_membership.Contains(item.Track.Id))
        {
            await _playlistService.RemoveTrackAsync(_playlistId, item.Track.Id);
            _membership.Remove(item.Track.Id);
        }
        else
        {
            await _playlistService.AddTrackAsync(_playlistId, item.Track.Id);
            _membership.Add(item.Track.Id);
        }

        item.Refresh(_membership.Contains(item.Track.Id));
        await ReloadTracksAsync();
    }

    [RelayCommand]
    private static async Task GoBackAsync() => await Shell.Current.GoToAsync("..");

    private async Task ReloadTracksAsync()
    {
        var tracks = await _playlistService.GetTracksAsync(_playlistId);
        Tracks.Clear();
        foreach (var track in tracks)
        {
            Tracks.Add(track);
        }

        OnPropertyChanged(nameof(HasTracks));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private async Task ReloadLibraryAsync()
    {
        var tracks = await _musicLibrary.GetTracksAsync();
        var membership = await _playlistService.GetTrackIdsAsync(_playlistId);
        _membership.Clear();
        foreach (var id in membership)
        {
            _membership.Add(id);
        }

        _allLibraryItems.Clear();
        foreach (var track in tracks.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            _allLibraryItems.Add(new LibraryTrackItem(track, _membership.Contains(track.Id)));
        }

        ApplySearch();
    }

    private void ApplySearch()
    {
        var query = SearchText.Trim();
        LibraryTracks.Clear();
        foreach (var item in _allLibraryItems)
        {
            if (string.IsNullOrEmpty(query)
                || item.Track.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || item.Track.Artist.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            {
                LibraryTracks.Add(item);
            }
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(EmptyMessage));
            OnPropertyChanged(nameof(AddTracksLabel));
            OnPropertyChanged(nameof(DoneLabel));
            OnPropertyChanged(nameof(PlayAllLabel));
            OnPropertyChanged(nameof(RemoveLabel));
            OnPropertyChanged(nameof(AddLabel));
            OnPropertyChanged(nameof(SearchPlaceholder));
            if (_isAllTracks)
            {
                PlaylistName = _localizationService["AllTracksPlaylist"];
                OnPropertyChanged(nameof(PlaylistName));
            }
        });

    // Deleting tracks (for example after removing a scan folder) must drop
    // them from an already open playlist instead of leaving stale rows that
    // can still be played.
    private void OnLibraryTracksChanged(object? sender, EventArgs eventArgs)
    {
        if (_playlistId <= 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => _ = ReloadAsync());
    }

    private async Task ReloadAsync()
    {
        await ReloadTracksAsync();
        await ReloadLibraryAsync();
    }
}

public sealed partial class LibraryTrackItem : ObservableObject
{
    public LibraryTrackItem(Track track, bool isInPlaylist)
    {
        Track = track;
        IsInPlaylist = isInPlaylist;
        Icon = isInPlaylist ? "check.png" : "add.png";
    }

    public Track Track { get; }

    [ObservableProperty]
    private bool isInPlaylist;

    [ObservableProperty]
    private string icon = string.Empty;

    partial void OnIsInPlaylistChanged(bool value) => Icon = value ? "check.png" : "add.png";

    public void Refresh(bool isInPlaylist)
    {
        IsInPlaylist = isInPlaylist;
        Icon = isInPlaylist ? "check.png" : "add.png";
    }
}
