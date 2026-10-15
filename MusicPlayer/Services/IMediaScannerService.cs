using MusicPlayer.Models;

namespace MusicPlayer.Services;

public interface IMediaScannerService
{
    IReadOnlyCollection<string> SupportedExtensions { get; }

    Task<IReadOnlyList<Track>> ScanFoldersAsync(
        IEnumerable<string> folders,
        CancellationToken cancellationToken = default);

    Task<Track?> PickAudioFileAsync(CancellationToken cancellationToken = default);
}