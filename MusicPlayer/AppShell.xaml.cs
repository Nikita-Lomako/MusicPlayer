using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Services;

namespace MusicPlayer
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            PlaylistsTab.Content = new Views.NavigationPlaceholderPage("PlaylistsTitle");
            HistoryTab.Content = new Views.NavigationPlaceholderPage("HistoryTitle");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<IThemeService>().ApplyCurrentTheme();
        }
    }
}
