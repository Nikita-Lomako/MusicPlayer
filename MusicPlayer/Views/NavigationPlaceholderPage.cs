using MusicPlayer.Services;

namespace MusicPlayer.Views;

public sealed class NavigationPlaceholderPage : ContentPage
{
    public NavigationPlaceholderPage(string titleKey)
    {
        var localizationService = LocalizationService.Instance;
        SetBinding(TitleProperty, new Binding($"[{titleKey}]", source: localizationService));

        var title = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontSize = 24
        };
        title.SetBinding(Label.TextProperty, new Binding($"[{titleKey}]", source: localizationService));

        var layout = new Grid();
        layout.Children.Add(title);
        Content = layout;
    }
}