using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MusicPlayer.Models;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly IAudioService _audioService;
    private readonly ILocalizationService _localizationService;
    private bool _hasInitialized;

    public HistoryViewModel(
        IHistoryService historyService,
        IAudioService audioService,
        ILocalizationService localizationService)
    {
        _historyService = historyService;
        _audioService = audioService;
        _localizationService = localizationService;

        _localizationService.LanguageChanged += OnLanguageChanged;
        _audioService.PropertyChanged += OnAudioServicePropertyChanged;
    }

    public ObservableCollection<HistoryGroup> Groups { get; } = [];

    public bool IsEmpty => Groups.All(group => group.Count == 0);

    public string Title => _localizationService["HistoryTitle"];

    public string EmptyMessage => _localizationService["EmptyHistory"];

    [ObservableProperty]
    private bool isBusy;

    public async Task InitializeAsync()
    {
        _hasInitialized = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        var queue = Groups.SelectMany(group => group).ToArray();
        var index = Array.IndexOf(queue, track);
        if (index >= 0)
        {
            await _audioService.SetQueueAsync(queue, index);
        }
    }

    [RelayCommand]
    private async Task OpenDetailsAsync(Track? track)
    {
        if (track is { Id: > 0 })
        {
            await Shell.Current.GoToAsync($"trackdetails?id={track.Id}");
        }
    }

    private async Task LoadAsync()
    {
        if (IsBusy || !_hasInitialized)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var recentlyAdded = await _historyService.GetRecentlyAddedAsync();
            var recentlyPlayed = await _historyService.GetRecentlyPlayedAsync();

            Groups.Clear();
            Groups.Add(new HistoryGroup(_localizationService["HistoryRecentlyAdded"], recentlyAdded));
            Groups.Add(new HistoryGroup(_localizationService["HistoryRecentlyPlayed"], recentlyPlayed));

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(EmptyMessage));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnAudioServicePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(IAudioService.CurrentTrack) && _hasInitialized)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(300);
                await LoadAsync();
            });
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(EmptyMessage));
            if (Groups.Count >= 2)
            {
                Groups[0].Title = _localizationService["HistoryRecentlyAdded"];
                Groups[1].Title = _localizationService["HistoryRecentlyPlayed"];
            }
        });
}

public sealed class HistoryGroup : List<Track>, System.ComponentModel.INotifyPropertyChanged
{
    private string _title;

    public HistoryGroup(string title, IEnumerable<Track> tracks) : base(tracks)
    {
        _title = title;
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Title)));
        }
    }
}
