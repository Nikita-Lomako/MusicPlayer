using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Models;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public sealed partial class PlayerViewModel : ObservableObject
{
    private readonly IAudioService _audioService;
    private readonly ILocalizationService _localizationService;

    private bool _suppressSeek;
    private double _seekValue;

    public PlayerViewModel(IAudioService audioService, ILocalizationService localizationService)
    {
        _audioService = audioService;
        _localizationService = localizationService;

        _audioService.PropertyChanged += OnAudioServicePropertyChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;

        _seekValue = SeekProgress;
    }

    public Track? CurrentTrack => _audioService.CurrentTrack;

    public bool HasTrack => CurrentTrack is not null;

    public string Title => CurrentTrack?.Title ?? _localizationService["NoTrackPlaying"];

    public string Artist => CurrentTrack?.Artist ?? string.Empty;

    public string Album => CurrentTrack?.Album ?? string.Empty;

    public bool HasArtwork => !string.IsNullOrWhiteSpace(CurrentTrack?.ArtworkPath);

    public string? ArtworkPath => CurrentTrack?.ArtworkPath;

    public bool IsPlaying => _audioService.IsPlaying;

    public string PlayPauseIcon => _audioService.IsPlaying ? "pause.png" : "play.png";

    public string PlayPauseLabel => _audioService.IsPlaying ? _localizationService["PauseButton"] : _localizationService["PlayButton"];

    public TimeSpan Position => _audioService.Position;

    public TimeSpan Duration => _audioService.Duration;

    public string PositionText => FormatTime(Position);

    public string DurationText => FormatTime(Duration);

    public double SeekProgress => Duration <= TimeSpan.Zero
        ? 0
        : Math.Clamp(Position.TotalMilliseconds / Duration.TotalMilliseconds, 0, 1);

    public double SeekValue
    {
        get => _seekValue;
        set
        {
            if (!SetProperty(ref _seekValue, value) || _suppressSeek)
            {
                return;
            }

            if (Duration > TimeSpan.Zero)
            {
                _ = _audioService.SeekAsync(TimeSpan.FromMilliseconds(Math.Clamp(value, 0, 1) * Duration.TotalMilliseconds));
            }
        }
    }

    public double Volume
    {
        get => _audioService.Volume;
        set => _audioService.SetVolume(value);
    }

    public bool IsShuffleEnabled => _audioService.IsShuffleEnabled;

    public string ShuffleIcon => _audioService.IsShuffleEnabled ? "shuffle.png" : "shuffle_off.png";

    public string ShuffleLabel => _audioService.IsShuffleEnabled
        ? _localizationService["ShuffleOn"]
        : _localizationService["ShuffleOff"];

    public PlaybackRepeatMode RepeatMode => _audioService.RepeatMode;

    public string RepeatIcon => _audioService.RepeatMode switch
    {
        PlaybackRepeatMode.RepeatTrack => "repeat_one.png",
        PlaybackRepeatMode.RepeatAll => "repeat.png",
        _ => "repeat_off.png"
    };

    public string RepeatLabel => _audioService.RepeatMode switch
    {
        PlaybackRepeatMode.RepeatTrack => _localizationService["RepeatTrack"],
        PlaybackRepeatMode.RepeatAll => _localizationService["RepeatAll"],
        _ => _localizationService["RepeatOff"]
    };

    public string PreviousLabel => _localizationService["PreviousButton"];

    public string NextLabel => _localizationService["NextButton"];

    public string VolumeLabel => _localizationService["VolumeLabel"];

    public string TrackInfoLabel => _localizationService["TrackInfoLabel"];

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
    private Task PreviousAsync() => _audioService.PreviousAsync();

    [RelayCommand]
    private void ToggleShuffle() => _audioService.SetShuffle(!_audioService.IsShuffleEnabled);

    [RelayCommand]
    private void CycleRepeat()
    {
        var next = _audioService.RepeatMode switch
        {
            PlaybackRepeatMode.None => PlaybackRepeatMode.RepeatAll,
            PlaybackRepeatMode.RepeatAll => PlaybackRepeatMode.RepeatTrack,
            _ => PlaybackRepeatMode.None
        };

        _audioService.SetRepeatMode(next);
    }

    [RelayCommand]
    private async Task OpenDetailsAsync()
    {
        if (CurrentTrack is { Id: > 0 } track)
        {
            await Shell.Current.GoToAsync($"trackdetails?id={track.Id}");
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs)
    {
        OnPropertyChanged(nameof(PlayPauseLabel));
        OnPropertyChanged(nameof(ShuffleLabel));
        OnPropertyChanged(nameof(RepeatLabel));
        OnPropertyChanged(nameof(PreviousLabel));
        OnPropertyChanged(nameof(NextLabel));
        OnPropertyChanged(nameof(VolumeLabel));
        OnPropertyChanged(nameof(TrackInfoLabel));
        OnPropertyChanged(nameof(Title));
    }

    private void OnAudioServicePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        switch (eventArgs.PropertyName)
        {
            case nameof(IAudioService.Position):
            case nameof(IAudioService.Duration):
                _suppressSeek = true;
                SeekValue = SeekProgress;
                _suppressSeek = false;
                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(PositionText));
                OnPropertyChanged(nameof(DurationText));
                break;
            case nameof(IAudioService.IsPlaying):
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(PlayPauseIcon));
                OnPropertyChanged(nameof(PlayPauseLabel));
                break;
            case nameof(IAudioService.CurrentTrack):
                OnPropertyChanged(nameof(CurrentTrack));
                OnPropertyChanged(nameof(HasTrack));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Artist));
                OnPropertyChanged(nameof(Album));
                OnPropertyChanged(nameof(ArtworkPath));
                OnPropertyChanged(nameof(HasArtwork));
                SeekValue = SeekProgress;
                break;
            case nameof(IAudioService.Volume):
                OnPropertyChanged(nameof(Volume));
                break;
            case nameof(IAudioService.IsShuffleEnabled):
                OnPropertyChanged(nameof(IsShuffleEnabled));
                OnPropertyChanged(nameof(ShuffleIcon));
                OnPropertyChanged(nameof(ShuffleLabel));
                break;
            case nameof(IAudioService.RepeatMode):
                OnPropertyChanged(nameof(RepeatMode));
                OnPropertyChanged(nameof(RepeatIcon));
                OnPropertyChanged(nameof(RepeatLabel));
                break;
        }
    }

    private static string FormatTime(TimeSpan value) =>
        value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");
}
