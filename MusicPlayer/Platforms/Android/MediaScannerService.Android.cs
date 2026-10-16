#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using Microsoft.Maui.Storage;
using MusicPlayer.Models;

namespace MusicPlayer.Services;

public sealed partial class MediaScannerService
{
    private static async Task<IReadOnlyList<Track>> ScanAndroidMediaStoreAsync(
        IReadOnlyCollection<string> folders,
        CancellationToken cancellationToken)
    {
        var resolver = Android.App.Application.Context.ContentResolver
            ?? throw new InvalidOperationException("Android ContentResolver is unavailable.");
        var mediaUri = global::Android.Provider.MediaStore.Audio.Media.ExternalContentUri
            ?? throw new InvalidOperationException("Android audio collection is unavailable.");
        var isMusicColumn = global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.IsMusic;
        var titleColumn = global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Title;
        var projection = new List<string>
        {
            global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Id,
            global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Title,
            global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Artist,
            global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Album,
            global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Duration,
            global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Size,
            global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.DisplayName,
            global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.DateAdded
        };

        var relativePathColumn = OperatingSystem.IsAndroidVersionAtLeast(29)
            ? global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.RelativePath
            : null;
        if (relativePathColumn is not null)
        {
            projection.Add(relativePathColumn);
        }

        var albumArtistColumn = OperatingSystem.IsAndroidVersionAtLeast(30)
            ? global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.AlbumArtist
            : null;
        if (albumArtistColumn is not null)
        {
            projection.Add(albumArtistColumn);
        }

        var relativeFolders = GetRelativeFolders(folders);
        using var cursor = resolver.Query(
            mediaUri,
            projection.ToArray(),
            $"{isMusicColumn} != 0",
            null,
            relativePathColumn is null ? $"{titleColumn} ASC" : $"{relativePathColumn} ASC, {titleColumn} ASC");

        if (cursor is null)
        {
            return Array.Empty<Track>();
        }

        var idIndex = cursor.GetColumnIndexOrThrow(global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Id);
        var titleIndex = cursor.GetColumnIndexOrThrow(global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Title);
        var artistIndex = cursor.GetColumnIndexOrThrow(global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Artist);
        var albumIndex = cursor.GetColumnIndexOrThrow(global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Album);
        var albumArtistIndex = albumArtistColumn is null ? -1 : cursor.GetColumnIndex(albumArtistColumn);
        var durationIndex = cursor.GetColumnIndexOrThrow(global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Duration);
        var sizeIndex = cursor.GetColumnIndexOrThrow(global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Size);
        var relativePathIndex = relativePathColumn is null ? -1 : cursor.GetColumnIndex(relativePathColumn);
        var displayNameIndex = cursor.GetColumnIndexOrThrow(global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.DisplayName);
        var dateAddedIndex = cursor.GetColumnIndexOrThrow(global::Android.Provider.MediaStore.Audio.Media.InterfaceConsts.DateAdded);

        var tracks = new List<Track>();
        while (cursor.MoveToNext())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var displayName = cursor.GetString(displayNameIndex) ?? string.Empty;
            var extension = Path.GetExtension(displayName);
            if (!IsSupportedExtension(extension))
            {
                continue;
            }

            var relativePath = relativePathIndex < 0 ? string.Empty : cursor.GetString(relativePathIndex) ?? string.Empty;
            if (!IsInSelectedFolder(relativePath, relativeFolders))
            {
                continue;
            }

            var id = cursor.GetLong(idIndex);
            var contentUri = ContentUris.WithAppendedId(mediaUri, id)?.ToString();
            if (string.IsNullOrWhiteSpace(contentUri))
            {
                continue;
            }
            var dateAddedSeconds = cursor.GetLong(dateAddedIndex);
            var track = new Track
            {
                FilePath = contentUri,
                Title = cursor.GetString(titleIndex) ?? Path.GetFileNameWithoutExtension(displayName),
                Artist = cursor.GetString(artistIndex) ?? string.Empty,
                Album = cursor.GetString(albumIndex),
                AlbumArtist = albumArtistIndex < 0 ? null : cursor.GetString(albumArtistIndex),
                Duration = TimeSpan.FromMilliseconds(cursor.GetLong(durationIndex)),
                FileSizeBytes = cursor.GetLong(sizeIndex),
                Format = extension.TrimStart('.').ToLowerInvariant(),
                AddedAtUtc = DateTimeOffset.FromUnixTimeSeconds(dateAddedSeconds).UtcDateTime,
                SourceFolder = NormalizeFolderKey(relativePath)
            };

            await EnrichFromTagLibAsync(resolver, contentUri, displayName, track, cancellationToken);
            tracks.Add(track);
        }

        return tracks;
    }

    private static string[] GetRelativeFolders(IEnumerable<string> folders)
    {
        return folders
            .Select(ComputeFolderKey)
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsInSelectedFolder(string relativePath, IReadOnlyCollection<string> folders)
    {
        // Nothing selected -> nothing to scan. Never fall back to every track
        // on the device just because the app can reach it.
        if (folders.Count == 0)
        {
            return false;
        }

        var candidate = NormalizeFolderKey(relativePath);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return folders.Any(folder => IsUnderFolder(candidate, folder));
    }

    private static bool IsUnderFolder(string candidate, string folder) =>
        candidate.Equals(folder, StringComparison.OrdinalIgnoreCase)
        || candidate.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);

    public string GetFolderKey(string folder) => ComputeFolderKey(folder);

    private static string ComputeFolderKey(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        var value = folder.Trim();

        // The picker normally returns a physical path, but older builds may
        // have stored the raw SAF tree/document URI. Normalize either form.
        if (value.Contains("://", StringComparison.Ordinal))
        {
            value = FromDocumentUri(value);
        }

        var externalStoragePath = global::Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
        if (!string.IsNullOrWhiteSpace(externalStoragePath)
            && value.StartsWith(externalStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            value = Path.GetRelativePath(externalStoragePath, value);
        }
        else
        {
            // Removable volume, e.g. /storage/1B0B-0B1C/Music -> Music.
            var match = System.Text.RegularExpressions.Regex.Match(
                value,
                "^/storage/(?<volume>[^/]+)/(?<rest>.+)$");
            if (match.Success
                && !match.Groups["volume"].Value.Equals("emulated", StringComparison.OrdinalIgnoreCase))
            {
                value = match.Groups["rest"].Value;
            }
        }

        return NormalizeFolderKey(value);
    }

    private static string FromDocumentUri(string uri)
    {
        var decoded = System.Uri.UnescapeDataString(uri);
        var treeIndex = decoded.IndexOf("/tree/", StringComparison.OrdinalIgnoreCase);
        if (treeIndex >= 0)
        {
            return StripVolumePrefix(decoded[(treeIndex + "/tree/".Length)..]);
        }

        var documentIndex = decoded.IndexOf("/document/", StringComparison.OrdinalIgnoreCase);
        if (documentIndex >= 0)
        {
            return StripVolumePrefix(decoded[(documentIndex + "/document/".Length)..]);
        }

        return decoded;
    }

    private static string StripVolumePrefix(string value)
    {
        var colonIndex = value.IndexOf(':');
        return colonIndex >= 0 ? value[(colonIndex + 1)..] : value;
    }

    private static async Task EnrichFromTagLibAsync(
        ContentResolver resolver,
        string contentUri,
        string displayName,
        Track track,
        CancellationToken cancellationToken)
    {
        var uri = global::Android.Net.Uri.Parse(contentUri);
        if (uri is null)
        {
            return;
        }
        using var source = resolver.OpenInputStream(uri);
        if (source is null)
        {
            return;
        }

        var temporaryPath = Path.Combine(
            FileSystem.CacheDirectory,
            $"{Guid.NewGuid():N}{Path.GetExtension(displayName).ToLowerInvariant()}");

        try
        {
            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            var tagTrack = await Task.Run(
                () => ReadTrack(temporaryPath, track.Title, track.FilePath, track.FileSizeBytes),
                cancellationToken);

            track.Title = tagTrack.Title;
            track.Artist = string.IsNullOrWhiteSpace(tagTrack.Artist) ? track.Artist : tagTrack.Artist;
            track.Album = string.IsNullOrWhiteSpace(tagTrack.Album) ? track.Album : tagTrack.Album;
            track.AlbumArtist = string.IsNullOrWhiteSpace(tagTrack.AlbumArtist) ? track.AlbumArtist : tagTrack.AlbumArtist;
            track.Duration = tagTrack.Duration > TimeSpan.Zero ? tagTrack.Duration : track.Duration;
            track.BitrateKbps = tagTrack.BitrateKbps;
            track.Year = tagTrack.Year;
            track.Genre = tagTrack.Genre;
            track.ArtworkPath = tagTrack.ArtworkPath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TagLib.CorruptFileException or TagLib.UnsupportedFormatException)
        {
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
#endif