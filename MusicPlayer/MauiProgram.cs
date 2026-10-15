using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MusicPlayer.Services;
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
#if MAUI_DEVFLOW
            builder.AddMauiDevFlowAgent();
#endif
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: true);

            builder.Services.AddSingleton<IMediaScannerService, MediaScannerService>();
            builder.Services.AddSingleton<IAudioService, AudioService>();

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            return builder.Build();
        }
    }
}
