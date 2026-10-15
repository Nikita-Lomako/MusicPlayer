using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MusicPlayer.Models;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public sealed partial class TrackDetailsViewModel : ObservableObject
{
    private readonly IMusicLibraryService _musicLibrary;
    private readonly IAudioService _audioService;
    private readonly ILocalizationService _localizationService;
    private Track? _track;
    private int _trackId;

    public TrackDetailsViewModel(
        IMusicLibraryService musicLibrary,
        IAudioService audioService,
        ILocalizationService localizationService)
    {
        _musicLibrary = musicLibrary;
        _audioService = audioService;
        _localizationService = localizationService;
        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public bool HasTrack => _track is not null;

    public string TrackTitle => _track?.Title ?? string.Empty;

    public string Artist => Value(_track?.Artist);

    public string Album => Value(_track?.Album);

    public string AlbumArtist => Value(_track?.AlbumArtist);

    public string Format => Value(_track?.Format?.ToUpperInvariant());

    public string Bitrate => _track?.BitrateKbps is > 0 ? $"{_track.BitrateKbps} kbps" : Unknown;

    public string FileSize => _track is null || _track.FileSizeBytes <= 0
        ? Unknown
        : FormatSize(_track.FileSizeBytes);

    public string FilePath => Value(_track?.FilePath);

    public string Year => _track?.Year is > 0 ? _track.Year.Value.ToString() : Unknown;

    public string Genre => Value(_track?.Genre);

    public string Duration => _track is null || _track.Duration <= TimeSpan.Zero
        ? Unknown
        : FormatTime(_track.Duration);

    public string ArtworkPath => _track?.ArtworkPath ?? string.Empty;

    public bool HasArtwork => !string.IsNullOrWhiteSpace(_track?.ArtworkPath);

    public string PageTitle => _localizationService["TrackDetailsTitle"];

    public string TitleLabel => _localizationService["DetailTitle"];

    public string ArtistLabel => _localizationService["DetailArtist"];

    public string AlbumLabel => _localizationService["DetailAlbum"];

    public string AlbumArtistLabel => _localizationService["DetailAlbumArtist"];

    public string FormatLabel => _localizationService["DetailFormat"];

    public string BitrateLabel => _localizationService["DetailBitrate"];

    public string FileSizeLabel => _localizationService["DetailSize"];

    public string FilePathLabel => _localizationService["DetailPath"];

    public string YearLabel => _localizationService["DetailYear"];

    public string GenreLabel => _localizationService["DetailGenre"];

    public string DurationLabel => _localizationService["DetailDuration"];

    public string PlayLabel => _localizationService["PlayButton"];

    private string Unknown => _localizationService["UnknownValue"];

    public async Task LoadAsync(int trackId)
    {
        _trackId = trackId;
        _track = await _musicLibrary.GetTrackAsync(trackId);
        NotifyAll();
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (_track is null)
        {
            return;
        }

        if (_audioService.CurrentTrack?.Id == _track.Id)
        {
            await _audioService.PlayAsync();
            return;
        }

        await _audioService.SetQueueAsync([_track], 0);
    }

    [RelayCommand]
    private static async Task GoBackAsync() => await Shell.Current.GoToAsync("..");

    private void OnLanguageChanged(object? sender, EventArgs eventArgs) =>
        MainThread.BeginInvokeOnMainThread(NotifyAll);

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(HasTrack));
        OnPropertyChanged(nameof(TrackTitle));
        OnPropertyChanged(nameof(Artist));
        OnPropertyChanged(nameof(Album));
        OnPropertyChanged(nameof(AlbumArtist));
        OnPropertyChanged(nameof(Format));
        OnPropertyChanged(nameof(Bitrate));
        OnPropertyChanged(nameof(FileSize));
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(Year));
        OnPropertyChanged(nameof(Genre));
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(ArtworkPath));
        OnPropertyChanged(nameof(HasArtwork));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(TitleLabel));
        OnPropertyChanged(nameof(ArtistLabel));
        OnPropertyChanged(nameof(AlbumLabel));
        OnPropertyChanged(nameof(AlbumArtistLabel));
        OnPropertyChanged(nameof(FormatLabel));
        OnPropertyChanged(nameof(BitrateLabel));
        OnPropertyChanged(nameof(FileSizeLabel));
        OnPropertyChanged(nameof(FilePathLabel));
        OnPropertyChanged(nameof(YearLabel));
        OnPropertyChanged(nameof(GenreLabel));
        OnPropertyChanged(nameof(DurationLabel));
        OnPropertyChanged(nameof(PlayLabel));
    }

    private string Value(string? value) => string.IsNullOrWhiteSpace(value) ? Unknown : value;

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    private static string FormatTime(TimeSpan value) =>
        value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");
}
