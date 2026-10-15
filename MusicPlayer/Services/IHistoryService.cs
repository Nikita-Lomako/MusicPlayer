using MusicPlayer.Models;

namespace MusicPlayer.Services;

/// <summary>
/// Tracks which files were discovered last and which tracks were played recently.
/// </summary>
public interface IHistoryService
{
    Task RecordPlayAsync(int trackId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> GetRecentlyPlayedAsync(int limit = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> GetRecentlyAddedAsync(int limit = 50, CancellationToken cancellationToken = default);
}
