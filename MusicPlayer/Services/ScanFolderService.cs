using System.Text.Json;
using Microsoft.Maui.Storage;

namespace MusicPlayer.Services;

public sealed class ScanFolderService : IScanFolderService
{
    private const string PreferenceKey = "scan_folders";
    private const string RemovedPreferenceKey = "removed_scan_folders";

    public IReadOnlyList<string> GetSelectedFolders() => ReadList(PreferenceKey);

    public IReadOnlyList<string> GetRemovedFolders() => ReadList(RemovedPreferenceKey);

    public IReadOnlyList<string> GetDefaultFolders()
    {
#if ANDROID
        return Array.Empty<string>();
#else
        var music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        return string.IsNullOrWhiteSpace(music) ? Array.Empty<string>() : [music];
#endif
    }

    public IReadOnlyList<string> GetEffectiveFolders()
    {
        var selected = GetSelectedFolders();
        return selected.Count > 0 ? selected : GetDefaultFolders();
    }

    public void AddFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        var trimmed = folder.Trim();
        var folders = GetSelectedFolders().ToList();
        if (!folders.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            folders.Add(trimmed);
            Save(PreferenceKey, folders);
        }

        // Re-adding a folder cancels a pending cleanup for it.
        var removed = GetRemovedFolders().ToList();
        if (removed.RemoveAll(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Save(RemovedPreferenceKey, removed);
        }
    }

    public void RemoveFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        var trimmed = folder.Trim();
        var folders = GetSelectedFolders().ToList();
        if (folders.RemoveAll(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Save(PreferenceKey, folders);

            // Remember the folder so the library can later drop the tracks it
            // contributed, without touching the folders that are still selected.
            var removed = GetRemovedFolders().ToList();
            if (!removed.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                removed.Add(trimmed);
                Save(RemovedPreferenceKey, removed);
            }
        }
    }

    public void ClearRemovedFolders() => Preferences.Default.Remove(RemovedPreferenceKey);

    private static IReadOnlyList<string> ReadList(string key)
    {
        var raw = Preferences.Default.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw) ?? [];
            return Normalize(values);
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static void Save(string key, IEnumerable<string> folders)
    {
        var payload = JsonSerializer.Serialize(folders.ToArray());
        Preferences.Default.Set(key, payload);
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string> folders) =>
        folders
            .Where(static folder => !string.IsNullOrWhiteSpace(folder))
            .Select(static folder => folder.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
