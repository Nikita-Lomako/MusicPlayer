using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views;

[QueryProperty(nameof(TrackId), "id")]
public partial class TrackDetailsPage : ContentPage
{
    private TrackDetailsViewModel? _viewModel;
    private int _trackId;

    public TrackDetailsPage()
    {
        InitializeComponent();
    }

    public string TrackId
    {
        set => _trackId = int.TryParse(value, out var id) ? id : 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel ??= Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<TrackDetailsViewModel>()
            ?? throw new InvalidOperationException("Track details view model is not registered.");
        BindingContext = _viewModel;

        if (_trackId > 0)
        {
            _ = _viewModel.LoadAsync(_trackId);
        }
    }
}
