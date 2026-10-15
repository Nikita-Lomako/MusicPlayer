using MusicPlayer.Models;

namespace MusicPlayer.Services;

/// <summary>
/// Persists the scanned / imported tracks so playlists and history stay valid between sessions.
/// </summary>
public interface IMusicLibraryService
{
    Task<IReadOnlyList<Track>> SaveTracksAsync(
        IEnumerable<Track> tracks,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> GetTracksAsync(CancellationToken cancellationToken = default);

    Task<Track?> GetTrackAsync(int trackId, CancellationToken cancellationToken = default);

    Task<Track?> FindByPathAsync(string filePath, CancellationToken cancellationToken = default);
}
