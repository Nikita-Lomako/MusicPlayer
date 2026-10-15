using MusicPlayer.Models;

namespace MusicPlayer.Services;

/// <summary>
/// CRUD operations for user playlists. The library always contains a protected
/// "all tracks" playlist that mirrors every track known to the device.
/// </summary>
public interface IPlaylistService
{
    Task<Playlist> GetOrCreateAllTracksPlaylistAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Playlist>> GetPlaylistsAsync(CancellationToken cancellationToken = default);

    Task<Playlist?> GetPlaylistAsync(int playlistId, CancellationToken cancellationToken = default);

    Task<Playlist> CreatePlaylistAsync(string name, CancellationToken cancellationToken = default);

    Task RenamePlaylistAsync(int playlistId, string name, CancellationToken cancellationToken = default);

    Task DeletePlaylistAsync(int playlistId, CancellationToken cancellationToken = default);

    Task<int> GetTrackCountAsync(int playlistId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> GetTracksAsync(int playlistId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetTrackIdsAsync(int playlistId, CancellationToken cancellationToken = default);

    Task AddTrackAsync(int playlistId, int trackId, CancellationToken cancellationToken = default);

    Task RemoveTrackAsync(int playlistId, int trackId, CancellationToken cancellationToken = default);
}
