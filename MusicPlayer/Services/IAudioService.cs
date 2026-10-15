using System.ComponentModel;
using CommunityToolkit.Maui.Views;
using MusicPlayer.Models;

namespace MusicPlayer.Services;

public interface IAudioService : INotifyPropertyChanged
{
    void AttachPlayer(MediaElement player);

    Track? CurrentTrack { get; }

    IReadOnlyList<Track> Queue { get; }

    int CurrentIndex { get; }

    bool IsPlaying { get; }

    TimeSpan Position { get; }

    TimeSpan Duration { get; }

    double Volume { get; }

    bool IsShuffleEnabled { get; }

    PlaybackRepeatMode RepeatMode { get; }

    Task SetQueueAsync(
        IReadOnlyList<Track> tracks,
        int startIndex = 0,
        bool playImmediately = true,
        CancellationToken cancellationToken = default);

    Task PlayAsync(CancellationToken cancellationToken = default);

    Task PauseAsync(CancellationToken cancellationToken = default);

    Task NextAsync(CancellationToken cancellationToken = default);

    Task PreviousAsync(CancellationToken cancellationToken = default);

    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

    void SetVolume(double volume);

    void SetShuffle(bool isEnabled);

    void SetRepeatMode(PlaybackRepeatMode mode);
}