using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicIsland.Services;

namespace DynamicIsland.ViewModels;

public enum IslandState
{
    Idle,
    MusicPlaying,
    Expanded
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private IslandState _currentState = IslandState.Idle;

    [ObservableProperty]
    private bool _isHovered;

    [ObservableProperty]
    private bool _isPinned;

    public MediaService Media { get; }

    public MainViewModel() : this(MediaService.Instance)
    {
    }

    public MainViewModel(MediaService mediaService)
    {
        Media = mediaService;
        Media.PropertyChanged += OnMediaPropertyChanged;
        UpdateState();
    }

    private void OnMediaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Media.IsPlaying) or nameof(Media.HasMedia) or nameof(Media.Title))
        {
            App.Current?.Dispatcher?.InvokeAsync(UpdateState);
        }
    }

    partial void OnIsHoveredChanged(bool value) => UpdateState();

    partial void OnIsPinnedChanged(bool value) => UpdateState();

    public void UpdateState()
    {
        if (IsHovered || IsPinned)
        {
            CurrentState = IslandState.Expanded;
        }
        else if (Media.HasMedia && (Media.IsPlaying || !string.IsNullOrWhiteSpace(Media.Title)))
        {
            CurrentState = IslandState.MusicPlaying;
        }
        else
        {
            CurrentState = IslandState.Idle;
        }
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        IsPinned = !IsPinned;
    }

    [RelayCommand]
    private void SetHovered(bool hovered)
    {
        IsHovered = hovered;
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        await Media.PlayPauseAsync();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        await Media.NextAsync();
    }

    [RelayCommand]
    private async Task PreviousAsync()
    {
        await Media.PreviousAsync();
    }
}
