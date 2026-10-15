using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views;

public partial class PlaylistsPage : ContentPage
{
    private PlaylistsViewModel? _viewModel;

    public PlaylistsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel ??= Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<PlaylistsViewModel>()
            ?? throw new InvalidOperationException("Playlists view model is not registered.");
        BindingContext = _viewModel;
        _ = _viewModel.InitializeAsync();
    }
}
