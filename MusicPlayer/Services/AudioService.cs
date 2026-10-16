using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using MusicPlayer.Models;

namespace MusicPlayer.Services;

public sealed class AudioService : IAudioService
{
    private readonly IHistoryService _historyService;
    private readonly IMusicLibraryService _musicLibrary;
    private readonly List<Track> _sourceQueue = [];
    private List<Track> _queue = [];
    private IReadOnlyList<Track> _queueView = Array.Empty<Track>();
    private MediaElement? _player;
    private int _currentIndex = -1;
    private bool _isPlaying;
    private bool _isShuffleEnabled;
    private bool _shouldPlayWhenOpened;
    private double _volume = 1;
    private TimeSpan _position;
    private TimeSpan _duration;
    private PlaybackRepeatMode _repeatMode;

    public AudioService(IHistoryService historyService, IMusicLibraryService musicLibrary)
    {
        _historyService = historyService;
        _musicLibrary = musicLibrary;
        _musicLibrary.TracksRemoved += OnTracksRemoved;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Track? CurrentTrack => _currentIndex >= 0 && _currentIndex < _queue.Count
        ? _queue[_currentIndex]
        : null;

    public IReadOnlyList<Track> Queue => _queueView;

    public int CurrentIndex => _currentIndex;

    public bool IsPlaying => _isPlaying;

    public TimeSpan Position => _position;

    public TimeSpan Duration => _duration > TimeSpan.Zero
        ? _duration
        : CurrentTrack?.Duration ?? TimeSpan.Zero;

    public double Volume => _volume;

    public bool IsShuffleEnabled => _isShuffleEnabled;

    public PlaybackRepeatMode RepeatMode => _repeatMode;

    public void AttachPlayer(MediaElement player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (ReferenceEquals(_player, player))
        {
            return;
        }

        if (_player is not null)
        {
            _player.MediaOpened -= OnMediaOpened;
            _player.MediaEnded -= OnMediaEnded;
            _player.PositionChanged -= OnPositionChanged;
            _player.StateChanged -= OnStateChanged;
            _player.MediaFailed -= OnMediaFailed;
        }

        _player = player;
        player.ShouldShowPlaybackControls = false;
        player.ShouldKeepScreenOn = false;
        player.ShouldLoopPlayback = false;
        player.Volume = _volume;
        player.MediaOpened += OnMediaOpened;
        player.MediaEnded += OnMediaEnded;
        player.PositionChanged += OnPositionChanged;
        player.StateChanged += OnStateChanged;
        player.MediaFailed += OnMediaFailed;

        if (CurrentTrack is not null)
        {
            player.ShouldAutoPlay = _isPlaying;
            player.Source = CreateSource(CurrentTrack.FilePath);
        }
    }

    public async Task SetQueueAsync(
        IReadOnlyList<Track> tracks,
        int startIndex = 0,
        bool playImmediately = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        cancellationToken.ThrowIfCancellationRequested();

        if (tracks.Count == 0)
        {
            _sourceQueue.Clear();
            _queue.Clear();
            _queueView = Array.Empty<Track>();
            _currentIndex = -1;
            _shouldPlayWhenOpened = false;
            _isPlaying = false;
            _position = TimeSpan.Zero;
            _duration = TimeSpan.Zero;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_player is not null)
                {
                    _player.ShouldAutoPlay = false;
                    _player.Stop();
                    _player.Source = null;
                }
            });

            NotifyQueueChanged();
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Duration));
            return;
        }

        if (startIndex < 0 || startIndex >= tracks.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        _sourceQueue.Clear();
        _sourceQueue.AddRange(tracks);
        BuildQueue(tracks[startIndex]);
        _position = TimeSpan.Zero;
        _duration = CurrentTrack?.Duration ?? TimeSpan.Zero;
        _shouldPlayWhenOpened = playImmediately;
        _isPlaying = false;

        var track = CurrentTrack!;
        if (playImmediately)
        {
            RecordPlayback(track);
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var player = GetPlayer();
            player.ShouldAutoPlay = playImmediately;
            player.Source = CreateSource(track.FilePath);
            player.MetadataTitle = track.Title;
            player.MetadataArtist = track.Artist;
            player.MetadataArtworkUrl = track.ArtworkPath ?? string.Empty;
        });

        NotifyQueueChanged();
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(Duration));
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (CurrentTrack is null)
        {
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            var player = GetPlayer();
            _shouldPlayWhenOpened = true;
            player.ShouldAutoPlay = true;
            if (player.CurrentState is MediaElementState.Paused or MediaElementState.Stopped)
            {
                TryPlay(player);
            }
        });
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _shouldPlayWhenOpened = false;

        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (_player is not null)
            {
                _player.ShouldAutoPlay = false;
                _player.Pause();
            }

            SetIsPlaying(false);
        });
    }

    public Task NextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_queue.Count == 0)
        {
            return Task.CompletedTask;
        }

        var nextIndex = _currentIndex + 1;
        if (nextIndex >= _queue.Count)
        {
            if (_repeatMode != PlaybackRepeatMode.RepeatAll)
            {
                _shouldPlayWhenOpened = false;
                return MainThread.InvokeOnMainThreadAsync(() =>
                {
                    GetPlayer().Stop();
                    SetIsPlaying(false);
                    _position = TimeSpan.Zero;
                    OnPropertyChanged(nameof(Position));
                });
            }

            nextIndex = 0;
        }

        return LoadTrackAsync(nextIndex, playImmediately: true, cancellationToken);
    }

    public Task PreviousAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_queue.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (Position > TimeSpan.FromSeconds(3))
        {
            return SeekAsync(TimeSpan.Zero, cancellationToken);
        }

        var previousIndex = _currentIndex - 1;
        if (previousIndex < 0)
        {
            previousIndex = _repeatMode == PlaybackRepeatMode.RepeatAll
                ? _queue.Count - 1
                : 0;
        }

        return LoadTrackAsync(previousIndex, playImmediately: true, cancellationToken);
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var player = GetPlayer();
        var target = position < TimeSpan.Zero ? TimeSpan.Zero : position;
        if (Duration > TimeSpan.Zero && target > Duration)
        {
            target = Duration;
        }

        await MainThread.InvokeOnMainThreadAsync(() => player.SeekTo(target, cancellationToken));
        _position = target;
        OnPropertyChanged(nameof(Position));
    }

    public void SetVolume(double volume)
    {
        if (!double.IsFinite(volume))
        {
            throw new ArgumentOutOfRangeException(nameof(volume));
        }

        _volume = Math.Clamp(volume, 0, 1);
        if (_player is not null)
        {
            MainThread.BeginInvokeOnMainThread(() => _player.Volume = _volume);
        }

        OnPropertyChanged(nameof(Volume));
    }

    public void SetShuffle(bool isEnabled)
    {
        if (_isShuffleEnabled == isEnabled)
        {
            return;
        }

        _isShuffleEnabled = isEnabled;
        if (CurrentTrack is not null)
        {
            BuildQueue(CurrentTrack);
            NotifyQueueChanged();
        }

        OnPropertyChanged(nameof(IsShuffleEnabled));
    }

    public void SetRepeatMode(PlaybackRepeatMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        _repeatMode = mode;
        OnPropertyChanged(nameof(RepeatMode));
    }

    private async Task LoadTrackAsync(int index, bool playImmediately, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _currentIndex = index;
        _position = TimeSpan.Zero;
        _duration = CurrentTrack?.Duration ?? TimeSpan.Zero;
        _shouldPlayWhenOpened = playImmediately;
        _isPlaying = false;
        var track = CurrentTrack!;

        if (playImmediately)
        {
            RecordPlayback(track);
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var player = GetPlayer();
            player.ShouldAutoPlay = playImmediately;
            player.Source = CreateSource(track.FilePath);
            player.MetadataTitle = track.Title;
            player.MetadataArtist = track.Artist;
            player.MetadataArtworkUrl = track.ArtworkPath ?? string.Empty;
        });

        OnPropertyChanged(nameof(CurrentTrack));
        OnPropertyChanged(nameof(CurrentIndex));
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(Duration));
    }

    private void BuildQueue(Track currentTrack)
    {
        if (!_isShuffleEnabled)
        {
            _queue = [.. _sourceQueue];
            _currentIndex = FindTrackIndex(_queue, currentTrack);
        }
        else
        {
            var remainingTracks = _sourceQueue
                .Where(track => !ReferenceEquals(track, currentTrack))
                .ToList();

            for (var index = remainingTracks.Count - 1; index > 0; index--)
            {
                var swapIndex = Random.Shared.Next(index + 1);
                (remainingTracks[index], remainingTracks[swapIndex]) = (remainingTracks[swapIndex], remainingTracks[index]);
            }

            _queue = [currentTrack, .. remainingTracks];
            _currentIndex = 0;
        }

        _queueView = _queue.AsReadOnly();
    }

    private static int FindTrackIndex(IReadOnlyList<Track> tracks, Track track)
    {
        for (var index = 0; index < tracks.Count; index++)
        {
            if (ReferenceEquals(tracks[index], track))
            {
                return index;
            }
        }

        return -1;
    }

    private void OnMediaOpened(object? sender, EventArgs eventArgs)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_player is null)
            {
                return;
            }

            _duration = _player.Duration;
            _position = _player.Position;
            if (_shouldPlayWhenOpened)
            {
                TryPlay(_player);
            }

            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(Position));
        });
    }

    private async void OnMediaEnded(object? sender, EventArgs eventArgs)
    {
        try
        {
            if (_repeatMode == PlaybackRepeatMode.RepeatTrack)
            {
                await SeekAsync(TimeSpan.Zero);
                await PlayAsync();
            }
            else
            {
                await NextAsync();
            }
        }
        catch (Exception)
        {
            SetIsPlaying(false);
        }
    }

    private void OnPositionChanged(object? sender, EventArgs eventArgs)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_player is null)
            {
                return;
            }

            _position = _player.Position;
            OnPropertyChanged(nameof(Position));
        });
    }

    private void OnStateChanged(object? sender, EventArgs eventArgs)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_player is not null)
            {
                SetIsPlaying(_player.CurrentState == MediaElementState.Playing);
            }
        });
    }

    private static void TryPlay(MediaElement player)
    {
        try
        {
            player.Play();
        }
        catch (Exception)
        {
            // The platform player can reject Play() while a source is still
            // initializing; ShouldAutoPlay will start playback on its own.
        }
    }

    private void OnMediaFailed(object? sender, MediaFailedEventArgs eventArgs)
    {
        _shouldPlayWhenOpened = false;
        SetIsPlaying(false);
    }

    private MediaElement GetPlayer() => _player
        ?? throw new InvalidOperationException("The audio player has not been attached to a MediaElement.");

    private void RecordPlayback(Track track)
    {
        if (track.Id <= 0)
        {
            return;
        }

        _ = _historyService.RecordPlayAsync(track.Id);
    }

    private static MediaSource CreateSource(string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out var uri)
        && (uri.Scheme.Equals("content", StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            ? MediaSource.FromUri(uri)
            : MediaSource.FromFile(path);

    private void OnTracksRemoved(object? sender, TracksRemovedEventArgs eventArgs)
    {
        if (eventArgs.TrackIds.Count == 0)
        {
            return;
        }

        var removed = new HashSet<int>(eventArgs.TrackIds);
        MainThread.BeginInvokeOnMainThread(() => RemoveTracksFromQueue(removed));
    }

    private void RemoveTracksFromQueue(HashSet<int> removed)
    {
        var current = CurrentTrack;
        var sourceRemoved = _sourceQueue.RemoveAll(track => removed.Contains(track.Id));
        var queueRemoved = _queue.RemoveAll(track => removed.Contains(track.Id));
        if (sourceRemoved == 0 && queueRemoved == 0)
        {
            return;
        }

        if (_queue.Count == 0)
        {
            // Nothing playable is left; reuse the existing reset path.
            _ = SetQueueAsync(Array.Empty<Track>());
            return;
        }

        var currentWasRemoved = current is not null && removed.Contains(current.Id);
        if (currentWasRemoved)
        {
            _currentIndex = Math.Clamp(_currentIndex, 0, _queue.Count - 1);
        }
        else
        {
            var index = FindTrackIndex(_queue, current!);
            _currentIndex = index >= 0 ? index : Math.Clamp(_currentIndex, 0, _queue.Count - 1);
        }

        _queueView = _queue.AsReadOnly();

        if (currentWasRemoved)
        {
            ReloadCurrentTrack();
        }

        NotifyQueueChanged();
    }

    private void ReloadCurrentTrack()
    {
        if (_player is null || CurrentTrack is not { } track)
        {
            return;
        }

        _position = TimeSpan.Zero;
        _duration = track.Duration;
        _player.ShouldAutoPlay = _shouldPlayWhenOpened;
        _player.Source = CreateSource(track.FilePath);
        _player.MetadataTitle = track.Title;
        _player.MetadataArtist = track.Artist;
        _player.MetadataArtworkUrl = track.ArtworkPath ?? string.Empty;
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(Duration));
    }

    private void NotifyQueueChanged()
    {
        OnPropertyChanged(nameof(Queue));
        OnPropertyChanged(nameof(CurrentTrack));
        OnPropertyChanged(nameof(CurrentIndex));
    }

    private void SetIsPlaying(bool value)
    {
        if (_isPlaying == value)
        {
            return;
        }

        _isPlaying = value;
        OnPropertyChanged(nameof(IsPlaying));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}