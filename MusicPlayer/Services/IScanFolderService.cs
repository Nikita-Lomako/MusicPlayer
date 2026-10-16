namespace MusicPlayer.Services;

/// <summary>
/// Stores the folders the user selected for scanning. The list is kept in
/// <see cref="Microsoft.Maui.Storage.Preferences"/> so it survives restarts.
/// </summary>
public interface IScanFolderService
{
    IReadOnlyList<string> GetSelectedFolders();

    /// <summary>
    /// Folders that should actually be scanned. Scanning is limited to the
    /// folders the user picked: when nothing is selected nothing is scanned,
    /// even if the app could reach other media on the device.
    /// </summary>
    IReadOnlyList<string> GetEffectiveFolders();

    /// <summary>
    /// Folders the user removed from the scan list. They are kept as a cache so
    /// the tracks they contributed can be purged from the library on demand.
    /// </summary>
    IReadOnlyList<string> GetRemovedFolders();

    void AddFolder(string folder);

    void RemoveFolder(string folder);

    void ClearRemovedFolders();
}
