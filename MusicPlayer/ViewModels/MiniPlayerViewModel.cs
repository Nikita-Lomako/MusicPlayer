using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MusicPlayer.Models;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

/// <summary>
/// Drives the reusable mini player shown at the bottom of the library,
/// playlists, playlist detail and history screens.
/// </summary>
public sealed partial class MiniPlayerViewModel : ObservableObject
{
    private readonly IAudioService _audioService;
    private readonly ILocalizationService _localizationService;

    public MiniPlayerViewModel(IAudioService audioService, ILocalizationService localizationService)
    {
        _audioService = audioService;
        _localizationService = localizationService;
        _audioService.PropertyChanged += OnAudioServicePropertyChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public Track? CurrentTrack => _audioService.CurrentTrack;

    public bool IsVisible => CurrentTrack is not null;

    public string Title => CurrentTrack?.Title ?? _localizationService["NoTrackPlaying"];

    public string Artist => CurrentTrack?.Artist ?? string.Empty;

    public string PlayPauseIcon => _audioService.IsPlaying ? "pause.png" : "play.png";

    public string PlayLabel => _localizationService["PlayButton"];

    public string NextLabel => _localizationService["NextButton"];

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
    private async Task OpenPlayerAsync()
    {
        if (CurrentTrack is not null)
        {
            await Shell.Current.GoToAsync("player");
        }
    }

    private void OnAudioServicePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(IAudioService.CurrentTrack)
            or nameof(IAudioService.IsPlaying))
        {
            OnPropertyChanged(nameof(CurrentTrack));
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Artist));
            OnPropertyChanged(nameof(PlayPauseIcon));
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(PlayLabel));
            OnPropertyChanged(nameof(NextLabel));
        });
}
