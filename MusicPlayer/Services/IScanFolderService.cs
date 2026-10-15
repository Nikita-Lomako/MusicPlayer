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

    void AddFolder(string folder);

    void RemoveFolder(string folder);
}
