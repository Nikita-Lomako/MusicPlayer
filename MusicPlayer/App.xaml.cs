using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Services;

namespace MusicPlayer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Apply the saved theme before the shell renders so there is no flash
            // of the wrong palette on startup.
            if (IPlatformApplication.Current?.Services.GetService<IThemeService>() is { } themeService)
            {
                themeService.ApplyCurrentTheme();
            }

            return window;
        }
    }
}
