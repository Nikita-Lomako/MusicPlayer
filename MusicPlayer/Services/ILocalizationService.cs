using System.ComponentModel;
using System.Globalization;

namespace MusicPlayer.Services;

public interface ILocalizationService : INotifyPropertyChanged
{
    CultureInfo CurrentCulture { get; }

    string CurrentLanguage { get; }

    string this[string key] { get; }

    event EventHandler? LanguageChanged;

    void SetLanguage(string languageCode);
}