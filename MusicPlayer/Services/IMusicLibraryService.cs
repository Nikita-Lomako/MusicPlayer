using MusicPlayer.Models;

namespace MusicPlayer.Services;

/// <summary>
/// Persists the scanned / imported tracks so playlists and history stay valid between sessions.
/// </summary>
public interface IMusicLibraryService
{
    /// <summary>
    /// Raised after tracks are inserted or updated. Observers reload their
    /// collections so a scan performed anywhere is reflected in the UI.
    /// </summary>
    event EventHandler? TracksChanged;

    /// <summary>
    /// Raised after tracks are deleted, carrying the ids of the removed
    /// tracks. The playback queue listens to this so a deleted track can no
    /// longer be reached with the next / previous controls.
    /// </summary>
    event EventHandler<TracksRemovedEventArgs>? TracksRemoved;

    Task<IReadOnlyList<Track>> SaveTracksAsync(
        IEnumerable<Track> tracks,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> GetTracksAsync(CancellationToken cancellationToken = default);

    Task<Track?> GetTrackAsync(int trackId, CancellationToken cancellationToken = default);

    Task<Track?> FindByPathAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every track located under the given normalized folder keys,
    /// together with its playlist links and history entries. Tracks coming from
    /// folders that are still selected are not touched. Returns the number of
    /// removed tracks and raises <see cref="TracksChanged"/> when it changed.
    /// </summary>
    Task<int> RemoveTracksFromFoldersAsync(
        IEnumerable<string> folderKeys,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Identifiers of the tracks that were just deleted from the library.
/// </summary>
public sealed class TracksRemovedEventArgs(IReadOnlyList<int> trackIds) : EventArgs
{
    public IReadOnlyList<int> TrackIds { get; } = trackIds;
}
