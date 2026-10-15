using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views;

[QueryProperty(nameof(PlaylistId), "id")]
public partial class PlaylistDetailPage : ContentPage
{
    private PlaylistDetailViewModel? _viewModel;
    private int _playlistId;

    public PlaylistDetailPage()
    {
        InitializeComponent();
    }

    public string PlaylistId
    {
        set => _playlistId = int.TryParse(value, out var id) ? id : 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel ??= Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<PlaylistDetailViewModel>()
            ?? throw new InvalidOperationException("Playlist detail view model is not registered.");
        BindingContext = _viewModel;

        if (_playlistId > 0)
        {
            _ = _viewModel.LoadAsync(_playlistId);
        }
    }
}
