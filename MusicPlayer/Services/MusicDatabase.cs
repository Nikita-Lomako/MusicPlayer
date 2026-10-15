using Microsoft.Maui.Storage;
using MusicPlayer.Models;
using SQLite;

namespace MusicPlayer.Services;

/// <summary>
/// Owns a single lazy-initialized <see cref="SQLiteAsyncConnection"/> shared by all data services.
/// </summary>
public sealed class MusicDatabase
{
    private const SQLiteOpenFlags OpenFlags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, "musicplayer.db3");

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _initializationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_connection is null)
            {
                var connection = new SQLiteAsyncConnection(DatabasePath, OpenFlags);
                await connection.CreateTableAsync<Track>().ConfigureAwait(false);
                await connection.CreateTableAsync<Playlist>().ConfigureAwait(false);
                await connection.CreateTableAsync<PlaylistTrack>().ConfigureAwait(false);
                await connection.CreateTableAsync<PlayHistoryEntry>().ConfigureAwait(false);
                _connection = connection;
            }
        }
        finally
        {
            _initializationGate.Release();
        }

        return _connection;
    }
}
