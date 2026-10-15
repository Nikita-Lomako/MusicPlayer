using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Services;

namespace MusicPlayer
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("player", typeof(Views.PlayerPage));
            Routing.RegisterRoute("playlist", typeof(Views.PlaylistDetailPage));
            Routing.RegisterRoute("trackdetails", typeof(Views.TrackDetailsPage));
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
            RefreshLocalizedTitles();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<IThemeService>().ApplyCurrentTheme();
        }

        private void OnLanguageChanged(object? sender, EventArgs eventArgs) =>
            Dispatcher.Dispatch(RefreshLocalizedTitles);

        private void RefreshLocalizedTitles()
        {
            var localization = LocalizationService.Instance;
            Title = localization.AppTitle;
            LibraryTab.Title = localization.LibraryTitle;
            PlaylistsTab.Title = localization.PlaylistsTitle;
            HistoryTab.Title = localization.HistoryTitle;
            SettingsTab.Title = localization.SettingsTitle;
        }
    }
}
