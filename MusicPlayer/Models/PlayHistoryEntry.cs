using SQLite;

namespace MusicPlayer.Models;

[Table("PlayHistory")]
public sealed class PlayHistoryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TrackId { get; set; }

    public DateTime PlayedAtUtc { get; set; } = DateTime.UtcNow;
}
