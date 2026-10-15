using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views;

public partial class HistoryPage : ContentPage
{
    private HistoryViewModel? _viewModel;

    public HistoryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel ??= Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<HistoryViewModel>()
            ?? throw new InvalidOperationException("History view model is not registered.");
        BindingContext = _viewModel;
        _ = _viewModel.InitializeAsync();
    }
}
