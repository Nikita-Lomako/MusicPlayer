namespace MusicPlayer.Services;

/// <summary>
/// Stores the folders the user selected for scanning. The list is kept in
/// <see cref="Microsoft.Maui.Storage.Preferences"/> so it survives restarts.
/// </summary>
public interface IScanFolderService
{
    IReadOnlyList<string> GetSelectedFolders();

    IReadOnlyList<string> GetDefaultFolders();

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
