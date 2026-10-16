using SQLite;

namespace MusicPlayer.Models;

[Table("Tracks")]
public sealed class Track
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique, MaxLength(2048)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Artist { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Album { get; set; }

    [MaxLength(512)]
    public string? AlbumArtist { get; set; }

    public long DurationTicks { get; set; }

    [Ignore]
    public TimeSpan Duration
    {
        get => TimeSpan.FromTicks(DurationTicks);
        set => DurationTicks = value.Ticks;
    }

    public long FileSizeBytes { get; set; }

    public int? BitrateKbps { get; set; }

    [MaxLength(16)]
    public string Format { get; set; } = string.Empty;

    public int? Year { get; set; }

    [MaxLength(512)]
    public string? Genre { get; set; }

    [MaxLength(2048)]
    public string? ArtworkPath { get; set; }

    /// <summary>
    /// Normalized key of the folder this track was scanned from. Used to drop
    /// the tracks contributed by a folder the user removed from the scan list.
    /// </summary>
    [MaxLength(1024)]
    public string? SourceFolder { get; set; }

    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
}