using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Services;

namespace MusicPlayer.Views;

public partial class LibraryPage : ContentPage
{
    private IAudioService? _audioService;

    public LibraryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _audioService ??= Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<IAudioService>()
            ?? throw new InvalidOperationException("Audio service is not registered.");
        _audioService.AttachPlayer(PlaybackElement);
    }
}