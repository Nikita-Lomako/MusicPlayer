using SQLite;

namespace MusicPlayer.Models;

[Table("Playlists")]
public sealed class Playlist
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? Description { get; set; }

    public bool IsAllTracks { get; set; }

    public bool IsSystemPlaylist { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}