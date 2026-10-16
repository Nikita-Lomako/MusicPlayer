using MusicPlayer.Models;

namespace MusicPlayer.Services;

public interface IMediaScannerService
{
    IReadOnlyCollection<string> SupportedExtensions { get; }

    Task<IReadOnlyList<Track>> ScanFoldersAsync(
        IEnumerable<string> folders,
        CancellationToken cancellationToken = default);

    Task<Track?> PickAudioFileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalizes a picked folder into the same key stored on
    /// <see cref="Track.SourceFolder"/>, so removed folders can be matched.
    /// </summary>
    string GetFolderKey(string folder);
}