using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MusicPlayer.Models;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public sealed partial class PlaylistsViewModel : ObservableObject
{
    private readonly IPlaylistService _playlistService;
    private readonly IMusicLibraryService _musicLibrary;
    private readonly ILocalizationService _localizationService;
    private bool _hasInitialized;
    private bool _reloadPending;

    public PlaylistsViewModel(
        IPlaylistService playlistService,
        IMusicLibraryService musicLibrary,
        ILocalizationService localizationService)
    {
        _playlistService = playlistService;
        _musicLibrary = musicLibrary;
        _localizationService = localizationService;
        _localizationService.LanguageChanged += OnLanguageChanged;
        _musicLibrary.TracksChanged += OnLibraryTracksChanged;
    }

    public ObservableCollection<PlaylistItem> Playlists { get; } = [];

    public bool HasPlaylists => Playlists.Count > 0;

    public string EmptyMessage => _localizationService["EmptyPlaylists"];

    public string Title => _localizationService["PlaylistsTitle"];

    public string NewPlaylistPlaceholder => _localizationService["NewPlaylistPlaceholder"];

    public string CreateLabel => _localizationService["CreatePlaylistButton"];

    public string DeleteLabel => _localizationService["DeletePlaylistLabel"];

    public string RenameLabel => _localizationService["RenamePlaylistLabel"];

    public string SaveLabel => _localizationService["SaveRenameButton"];

    public string CancelLabel => _localizationService["CancelButton"];

    [ObservableProperty]
    private string newPlaylistName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    private PlaylistItem? renamingPlaylist;

    [ObservableProperty]
    private string renameText = string.Empty;

    public bool IsRenaming => RenamingPlaylist is not null;

    partial void OnRenamingPlaylistChanged(PlaylistItem? value) => OnPropertyChanged(nameof(IsRenaming));

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    public async Task InitializeAsync()
    {
        if (_hasInitialized)
        {
            await LoadAsync();
            return;
        }

        _hasInitialized = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CreateAsync()
    {
        var name = NewPlaylistName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = _localizationService["PlaylistNameRequired"];
            return;
        }

        ErrorMessage = null;
        await _playlistService.CreatePlaylistAsync(name);
        NewPlaylistName = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(PlaylistItem? item)
    {
        if (item is null || item.IsSystem)
        {
            return;
        }

        var confirmed = await Shell.Current.DisplayAlertAsync(
            _localizationService["DeleteConfirmTitle"],
            _localizationService["DeleteConfirmMessage"],
            _localizationService["ConfirmButton"],
            _localizationService["CancelButton"]);

        if (!confirmed)
        {
            return;
        }

        await _playlistService.DeletePlaylistAsync(item.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private void BeginRename(PlaylistItem? item)
    {
        if (item is null || item.IsSystem)
        {
            return;
        }

        RenamingPlaylist = item;
        RenameText = item.Name;
    }

    [RelayCommand]
    private void CancelRename()
    {
        RenamingPlaylist = null;
        RenameText = string.Empty;
    }

    [RelayCommand]
    private async Task SaveRenameAsync()
    {
        if (RenamingPlaylist is null)
        {
            return;
        }

        var name = RenameText.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = _localizationService["PlaylistNameRequired"];
            return;
        }

        ErrorMessage = null;
        await _playlistService.RenamePlaylistAsync(RenamingPlaylist.Id, name);
        CancelRename();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenAsync(PlaylistItem? item)
    {
        if (item is null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"playlist?id={item.Id}");
    }

    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            _reloadPending = true;
            return;
        }

        IsBusy = true;
        try
        {
            await _playlistService.GetOrCreateAllTracksPlaylistAsync();
            var playlists = await _playlistService.GetPlaylistsAsync();

            Playlists.Clear();
            foreach (var playlist in playlists)
            {
                var count = await _playlistService.GetTrackCountAsync(playlist.Id);
                Playlists.Add(new PlaylistItem(playlist, count, _localizationService));
            }

            OnPropertyChanged(nameof(HasPlaylists));
            OnPropertyChanged(nameof(EmptyMessage));
        }
        finally
        {
            IsBusy = false;
            if (_reloadPending)
            {
                _reloadPending = false;
                await LoadAsync();
            }
        }
    }

    private void OnLibraryTracksChanged(object? sender, EventArgs eventArgs) =>
        MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync());

    private void OnLanguageChanged(object? sender, EventArgs eventArgs) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(EmptyMessage));
            OnPropertyChanged(nameof(NewPlaylistPlaceholder));
            OnPropertyChanged(nameof(CreateLabel));
            OnPropertyChanged(nameof(DeleteLabel));
            OnPropertyChanged(nameof(RenameLabel));
            OnPropertyChanged(nameof(SaveLabel));
            OnPropertyChanged(nameof(CancelLabel));
            foreach (var item in Playlists)
            {
                item.Refresh(_localizationService);
            }
        });
}

public sealed partial class PlaylistItem : ObservableObject
{
    private readonly Playlist _playlist;

    public PlaylistItem(Playlist playlist, int trackCount, ILocalizationService localization)
    {
        _playlist = playlist;
        TrackCount = trackCount;
        Refresh(localization);
    }

    public int Id => _playlist.Id;

    public string Name => _playlist.Name;

    public bool IsSystem => _playlist.IsSystemPlaylist;

    public int TrackCount { get; }

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string trackCountText = string.Empty;

    public void Refresh(ILocalizationService localization)
    {
        DisplayName = _playlist.IsAllTracks ? localization["AllTracksPlaylist"] : _playlist.Name;
        TrackCountText = $"{localization["TrackCountLabel"]} {TrackCount}";
    }
}
