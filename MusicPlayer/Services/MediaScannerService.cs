using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using MusicPlayer.Models;

namespace MusicPlayer.Services;

public sealed partial class MediaScannerService : IMediaScannerService
{
    private static readonly string[] AudioExtensions = [".mp3", ".m4a", ".flac", ".wav"];
    private static readonly IReadOnlyCollection<string> ReadOnlyAudioExtensions = Array.AsReadOnly(AudioExtensions);

    private static readonly FilePickerFileType AudioFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["audio/mpeg", "audio/mp4", "audio/flac", "audio/wav", "audio/x-wav"],
        [DevicePlatform.iOS] = ["public.mp3", "public.mpeg-4-audio", "org.xiph.flac", "com.microsoft.waveform-audio"],
        [DevicePlatform.MacCatalyst] = ["public.mp3", "public.mpeg-4-audio", "org.xiph.flac", "com.microsoft.waveform-audio"],
        [DevicePlatform.WinUI] = AudioExtensions
    });

    private readonly ILocalizationService _localizationService;

    public MediaScannerService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public IReadOnlyCollection<string> SupportedExtensions => ReadOnlyAudioExtensions;

    public async Task<IReadOnlyList<Track>> ScanFoldersAsync(
        IEnumerable<string> folders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folders);

#if ANDROID
        var permissionStatus = await MainThread.InvokeOnMainThreadAsync(
            () => Permissions.RequestAsync<ReadAudioPermission>());

        if (permissionStatus != PermissionStatus.Granted)
        {
            return Array.Empty<Track>();
        }
#endif

        var folderList = folders
            .Where(static folder => !string.IsNullOrWhiteSpace(folder))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    #if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            return await Task.Run(
                () => ScanFolders(folderList, cancellationToken),
                cancellationToken);
        }

        return await ScanAndroidMediaStoreAsync(folderList, cancellationToken);
    #else
        return await Task.Run(
            () => ScanFolders(folderList, cancellationToken),
            cancellationToken);
    #endif
    }

    public async Task<Track?> PickAudioFileAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await MainThread.InvokeOnMainThreadAsync(
            () => FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = _localizationService["AudioFilePickerTitle"],
                FileTypes = AudioFileType
            }));

        if (result is null)
        {
            return null;
        }

        var extension = Path.GetExtension(result.FileName);
        if (!IsSupportedExtension(extension))
        {
            throw new InvalidDataException(string.Format(
                _localizationService["UnsupportedFormat"],
                extension));
        }

        var importDirectory = Path.Combine(FileSystem.AppDataDirectory, "ImportedAudio");
        Directory.CreateDirectory(importDirectory);

        var importedPath = Path.Combine(importDirectory, $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");

        try
        {
            await using (var source = await result.OpenReadAsync())
            await using (var destination = new FileStream(
                importedPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            return await Task.Run(
                () => ReadTrack(importedPath, Path.GetFileNameWithoutExtension(result.FileName)),
                cancellationToken);
        }
        catch
        {
            File.Delete(importedPath);
            throw;
        }
    }

    private static IReadOnlyList<Track> ScanFolders(
        IEnumerable<string> folders,
        CancellationToken cancellationToken)
    {
        var tracks = new List<Track>();
        var scannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        foreach (var folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(folder))
            {
                continue;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folder, "*", enumerationOptions);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                continue;
            }

            try
            {
                foreach (var filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!IsSupportedExtension(Path.GetExtension(filePath)) || !scannedPaths.Add(filePath))
                    {
                        continue;
                    }

                    try
                    {
                        var track = ReadTrack(filePath, Path.GetFileNameWithoutExtension(filePath));
                        track.SourceFolder = NormalizeFolderKey(Path.GetDirectoryName(filePath));
                        tracks.Add(track);
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or TagLib.CorruptFileException or TagLib.UnsupportedFormatException)
                    {
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
            }
        }

        return tracks;
    }

    private static Track ReadTrack(
        string filePath,
        string fallbackTitle,
        string? playbackPath = null,
        long? fileSizeBytes = null)
    {
        using var file = TagLib.File.Create(filePath, TagLib.ReadStyle.Average);
        var tag = file.Tag;
        var properties = file.Properties;
        var fileInfo = new FileInfo(filePath);

        var track = new Track
        {
            FilePath = playbackPath ?? filePath,
            Title = string.IsNullOrWhiteSpace(tag.Title) ? fallbackTitle : tag.Title,
            Artist = tag.Performers.FirstOrDefault() ?? string.Empty,
            Album = tag.Album,
            AlbumArtist = tag.AlbumArtists.FirstOrDefault(),
            Duration = properties.Duration,
            FileSizeBytes = fileSizeBytes ?? fileInfo.Length,
            BitrateKbps = properties.AudioBitrate > 0 ? properties.AudioBitrate : null,
            Format = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
            Year = tag.Year > 0 ? (int)tag.Year : null,
            Genre = tag.Genres.FirstOrDefault()
        };

        SaveArtwork(track.FilePath, tag.Pictures.FirstOrDefault(), track);
        return track;
    }

    private static void SaveArtwork(string sourcePath, TagLib.IPicture? picture, Track track)
    {
        if (picture is null || picture.Data.Data.Length == 0)
        {
            return;
        }

        var extension = picture.MimeType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourcePath)));
        var artworkDirectory = Path.Combine(FileSystem.AppDataDirectory, "Artwork");
        Directory.CreateDirectory(artworkDirectory);

        var artworkPath = Path.Combine(artworkDirectory, $"{hash}{extension}");
        if (!File.Exists(artworkPath))
        {
            File.WriteAllBytes(artworkPath, picture.Data.Data);
        }

        track.ArtworkPath = artworkPath;
    }

    private static bool IsSupportedExtension(string extension) =>
        AudioExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Canonical folder key used on <see cref="Track.SourceFolder"/>. Forward
    /// slashes and no trailing separator so comparisons are platform agnostic.
    /// </summary>
    internal static string NormalizeFolderKey(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        return folder.Trim().Replace('\\', '/').Trim('/');
    }

#if !ANDROID
    public string GetFolderKey(string folder) => NormalizeFolderKey(folder);
#endif

#if ANDROID
    private sealed class ReadAudioPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            OperatingSystem.IsAndroidVersionAtLeast(33)
                ? [(global::Android.Manifest.Permission.ReadMediaAudio, true)]
                : [(global::Android.Manifest.Permission.ReadExternalStorage, true)];
    }
#endif
}