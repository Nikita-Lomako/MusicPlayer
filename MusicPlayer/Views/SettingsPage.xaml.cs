using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext ??= Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<SettingsViewModel>()
            ?? throw new InvalidOperationException("Settings view model is not registered.");
    }
}