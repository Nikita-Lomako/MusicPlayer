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

    Task<IReadOnlyList<Track>> SaveTracksAsync(
        IEnumerable<Track> tracks,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> GetTracksAsync(CancellationToken cancellationToken = default);

    Task<Track?> GetTrackAsync(int trackId, CancellationToken cancellationToken = default);

    Task<Track?> FindByPathAsync(string filePath, CancellationToken cancellationToken = default);
}
