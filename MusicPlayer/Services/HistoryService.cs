using MusicPlayer.Models;

namespace MusicPlayer.Services;

public sealed class HistoryService : IHistoryService
{
    private const int MaxEntries = 500;

    private readonly MusicDatabase _database;

    public HistoryService(MusicDatabase database) => _database = database;

    public async Task RecordPlayAsync(int trackId, CancellationToken cancellationToken = default)
    {
        if (trackId <= 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.InsertAsync(new PlayHistoryEntry
        {
            TrackId = trackId,
            PlayedAtUtc = DateTime.UtcNow
        }).ConfigureAwait(false);

        var count = await connection.Table<PlayHistoryEntry>().CountAsync().ConfigureAwait(false);
        if (count > MaxEntries)
        {
            var stale = await connection.Table<PlayHistoryEntry>()
                .OrderBy(entry => entry.PlayedAtUtc)
                .Take(count - MaxEntries)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var entry in stale)
            {
                await connection.DeleteAsync(entry).ConfigureAwait(false);
            }
        }
    }

    public async Task<IReadOnlyList<Track>> GetRecentlyPlayedAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        var entries = await connection.Table<PlayHistoryEntry>()
            .OrderByDescending(entry => entry.PlayedAtUtc)
            .ToListAsync()
            .ConfigureAwait(false);

        var tracks = await connection.Table<Track>().ToListAsync().ConfigureAwait(false);
        var byId = tracks.ToDictionary(track => track.Id);

        var result = new List<Track>();
        var seen = new HashSet<int>();
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (seen.Add(entry.TrackId) && byId.TryGetValue(entry.TrackId, out var track))
            {
                result.Add(track);
                if (result.Count >= limit)
                {
                    break;
                }
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<Track>> GetRecentlyAddedAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<Track>()
            .OrderByDescending(track => track.AddedAtUtc)
            .Take(limit)
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
