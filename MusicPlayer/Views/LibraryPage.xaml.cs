using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Services;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views;

public partial class LibraryPage : ContentPage
{
    private IAudioService? _audioService;
    private LibraryViewModel? _viewModel;

    public LibraryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("App service provider is unavailable.");
        _audioService ??= services.GetRequiredService<IAudioService>();
        _viewModel ??= services.GetRequiredService<LibraryViewModel>();
        BindingContext = _viewModel;
        _audioService.AttachPlayer(PlaybackElement);
        _ = _viewModel.InitializeAsync();
    }

    private void OnSearchButtonPressed(object? sender, EventArgs eventArgs) => SearchInput.Unfocus();
}