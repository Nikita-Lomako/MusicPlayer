using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views;

public partial class PlayerPage : ContentPage
{
    public PlayerPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext ??= Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<PlayerViewModel>()
            ?? throw new InvalidOperationException("Player view model is not registered.");
    }
}
