using System.Text.Json;
using Microsoft.Maui.Storage;

namespace MusicPlayer.Services;

public sealed class ScanFolderService : IScanFolderService
{
    private const string PreferenceKey = "scan_folders";

    public IReadOnlyList<string> GetSelectedFolders()
    {
        var raw = Preferences.Default.Get(PreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        try
        {
            var folders = JsonSerializer.Deserialize<string[]>(raw) ?? [];
            return Normalize(folders);
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

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

        var folders = GetSelectedFolders().ToList();
        if (folders.Contains(folder, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        folders.Add(folder.Trim());
        Save(folders);
    }

    public void RemoveFolder(string folder)
    {
        var folders = GetSelectedFolders().ToList();
        var removed = folders.RemoveAll(item => string.Equals(item, folder, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            Save(folders);
        }
    }

    private static void Save(IEnumerable<string> folders)
    {
        var payload = JsonSerializer.Serialize(folders.ToArray());
        Preferences.Default.Set(PreferenceKey, payload);
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string> folders) =>
        folders
            .Where(static folder => !string.IsNullOrWhiteSpace(folder))
            .Select(static folder => folder.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
