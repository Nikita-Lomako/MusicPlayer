using MusicPlayer.Models;

namespace MusicPlayer.Services;

public sealed class PlaylistService : IPlaylistService
{
    private readonly MusicDatabase _database;

    public PlaylistService(MusicDatabase database) => _database = database;

    public async Task<Playlist> GetOrCreateAllTracksPlaylistAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlist = await connection.Table<Playlist>()
            .Where(item => item.IsAllTracks)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (playlist is not null)
        {
            return playlist;
        }

        playlist = new Playlist
        {
            Name = "All tracks",
            IsAllTracks = true,
            IsSystemPlaylist = true
        };

        await connection.InsertAsync(playlist).ConfigureAwait(false);
        return playlist;
    }

    public async Task<IReadOnlyList<Playlist>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        await GetOrCreateAllTracksPlaylistAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlists = await connection.Table<Playlist>().ToListAsync().ConfigureAwait(false);
        return playlists
            .OrderByDescending(item => item.IsAllTracks)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public async Task<Playlist?> GetPlaylistAsync(int playlistId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<Playlist>()
            .Where(item => item.Id == playlistId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<Playlist> CreatePlaylistAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlist = new Playlist { Name = name.Trim() };
        await connection.InsertAsync(playlist).ConfigureAwait(false);
        return playlist;
    }

    public async Task RenamePlaylistAsync(int playlistId, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlist = await connection.Table<Playlist>()
            .Where(item => item.Id == playlistId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (playlist is null || playlist.IsSystemPlaylist)
        {
            return;
        }

        playlist.Name = name.Trim();
        await connection.UpdateAsync(playlist).ConfigureAwait(false);
    }

    public async Task DeletePlaylistAsync(int playlistId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlist = await connection.Table<Playlist>()
            .Where(item => item.Id == playlistId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (playlist is null || playlist.IsSystemPlaylist)
        {
            return;
        }

        await connection.Table<PlaylistTrack>()
            .Where(item => item.PlaylistId == playlistId)
            .DeleteAsync()
            .ConfigureAwait(false);
        await connection.DeleteAsync(playlist).ConfigureAwait(false);
    }

    public async Task<int> GetTrackCountAsync(int playlistId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlist = await connection.Table<Playlist>()
            .Where(item => item.Id == playlistId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (playlist is null)
        {
            return 0;
        }

        if (playlist.IsAllTracks)
        {
            return await connection.Table<Track>().CountAsync().ConfigureAwait(false);
        }

        return await connection.Table<PlaylistTrack>()
            .Where(item => item.PlaylistId == playlistId)
            .CountAsync()
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Track>> GetTracksAsync(int playlistId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlist = await connection.Table<Playlist>()
            .Where(item => item.Id == playlistId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (playlist is null)
        {
            return Array.Empty<Track>();
        }

        var tracks = await connection.Table<Track>().ToListAsync().ConfigureAwait(false);
        var byId = tracks.ToDictionary(track => track.Id);

        if (playlist.IsAllTracks)
        {
            return tracks
                .OrderBy(track => track.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }

        var links = await connection.Table<PlaylistTrack>()
            .Where(item => item.PlaylistId == playlistId)
            .ToListAsync()
            .ConfigureAwait(false);

        return links
            .OrderBy(link => link.Position)
            .ThenBy(link => link.Id)
            .Where(link => byId.ContainsKey(link.TrackId))
            .Select(link => byId[link.TrackId])
            .ToArray();
    }

    public async Task<IReadOnlyList<int>> GetTrackIdsAsync(int playlistId, CancellationToken cancellationToken = default)
    {
        var tracks = await GetTracksAsync(playlistId, cancellationToken).ConfigureAwait(false);
        return tracks.Select(track => track.Id).ToArray();
    }

    public async Task AddTrackAsync(int playlistId, int trackId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlist = await connection.Table<Playlist>()
            .Where(item => item.Id == playlistId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (playlist is null || playlist.IsAllTracks)
        {
            return;
        }

        var existing = await connection.Table<PlaylistTrack>()
            .Where(item => item.PlaylistId == playlistId && item.TrackId == trackId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return;
        }

        var position = await connection.Table<PlaylistTrack>()
            .Where(item => item.PlaylistId == playlistId)
            .CountAsync()
            .ConfigureAwait(false);

        await connection.InsertAsync(new PlaylistTrack
        {
            PlaylistId = playlistId,
            TrackId = trackId,
            Position = position
        }).ConfigureAwait(false);
    }

    public async Task RemoveTrackAsync(int playlistId, int trackId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var playlist = await connection.Table<Playlist>()
            .Where(item => item.Id == playlistId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (playlist is null || playlist.IsAllTracks)
        {
            return;
        }

        await connection.Table<PlaylistTrack>()
            .Where(item => item.PlaylistId == playlistId && item.TrackId == trackId)
            .DeleteAsync()
            .ConfigureAwait(false);
    }
}
