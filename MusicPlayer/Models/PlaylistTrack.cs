using SQLite;

namespace MusicPlayer.Models;

[Table("PlaylistTracks")]
public sealed class PlaylistTrack
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int PlaylistId { get; set; }

    public int TrackId { get; set; }

    public int Position { get; set; }
}