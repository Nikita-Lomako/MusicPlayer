using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views.Controls;

public partial class MiniPlayerView : ContentView
{
    public MiniPlayerView()
    {
        InitializeComponent();
        ResolveViewModel();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        ResolveViewModel();
    }

    private void ResolveViewModel()
    {
        if (BindingContext is MiniPlayerViewModel)
        {
            return;
        }

        var services = IPlatformApplication.Current?.Services
            ?? Application.Current?.Handler?.MauiContext?.Services;

        if (services is not null)
        {
            BindingContext = services.GetRequiredService<MiniPlayerViewModel>();
        }
    }
}
