using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MusicPlayer.Services;
using MusicPlayer.ViewModels;
#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace MusicPlayer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            var localizationService = new LocalizationService();
            builder.Services.AddSingleton<ILocalizationService>(localizationService);
            builder.Services.AddSingleton(localizationService);
            builder.Services.AddSingleton<IThemeService, ThemeService>();
#if MAUI_DEVFLOW
            builder.AddMauiDevFlowAgent();
#endif
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: true);

            builder.Services.AddSingleton<IMediaScannerService, MediaScannerService>();
            builder.Services.AddSingleton<MusicDatabase>();
            builder.Services.AddSingleton<IMusicLibraryService, MusicLibraryService>();
            builder.Services.AddSingleton<IPlaylistService, PlaylistService>();
            builder.Services.AddSingleton<IHistoryService, HistoryService>();
            builder.Services.AddSingleton<IScanFolderService, ScanFolderService>();
            builder.Services.AddSingleton<IAudioService, AudioService>();
            builder.Services.AddSingleton<LibraryViewModel>();
            builder.Services.AddSingleton<PlayerViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddSingleton<PlaylistsViewModel>();
            builder.Services.AddTransient<PlaylistDetailViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<TrackDetailsViewModel>();

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            return builder.Build();
        }
    }
}
