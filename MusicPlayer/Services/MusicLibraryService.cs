using MusicPlayer.Models;

namespace MusicPlayer.Services;

public sealed class MusicLibraryService : IMusicLibraryService
{
    private readonly MusicDatabase _database;

    public MusicLibraryService(MusicDatabase database) => _database = database;

    public event EventHandler? TracksChanged;

    public async Task<IReadOnlyList<Track>> SaveTracksAsync(
        IEnumerable<Track> tracks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var saved = new List<Track>();

        foreach (var track in tracks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(track.FilePath))
            {
                continue;
            }

            var existing = await connection.Table<Track>()
                .Where(item => item.FilePath == track.FilePath)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (existing is null)
            {
                if (track.AddedAtUtc == default)
                {
                    track.AddedAtUtc = DateTime.UtcNow;
                }

                await connection.InsertAsync(track).ConfigureAwait(false);
            }
            else
            {
                track.Id = existing.Id;
                track.AddedAtUtc = existing.AddedAtUtc;
                await connection.UpdateAsync(track).ConfigureAwait(false);
            }

            saved.Add(track);
        }

        if (saved.Count > 0)
        {
            TracksChanged?.Invoke(this, EventArgs.Empty);
        }

        return saved;
    }

    public async Task<IReadOnlyList<Track>> GetTracksAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<Track>().ToListAsync().ConfigureAwait(false);
    }

    public async Task<Track?> GetTrackAsync(int trackId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<Track>()
            .Where(track => track.Id == trackId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<Track?> FindByPathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<Track>()
            .Where(track => track.FilePath == filePath)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }
}
