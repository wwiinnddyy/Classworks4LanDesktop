using ClassworksPlugin.Services;
using LanMountainDesktop.AirAppSdk;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading;
using Avalonia.Media;
using Avalonia;

namespace ClassworksPlugin.Widgets;

public partial class ClassworksHomeworkWidget : AirAppWidgetBase
{
    private readonly ClassworksHomeworkViewModel _viewModel;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isDarkMode;

    public ClassworksHomeworkWidget(
        ClassworksSettingsService settingsService,
        ClassworksService classworksService)
    {
        InitializeComponent();
        _viewModel = new ClassworksHomeworkViewModel(settingsService, classworksService);
        DataContext = _viewModel;

        ActualThemeVariantChanged += (_, _) => UpdateTheme();
    }

    protected override void OnAttachedCore()
    {
        UpdateTheme();
        _ = _viewModel.LoadAssignmentsAsync();
    }

    protected override void OnDetachedCore()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }

    protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
    {
        var newIsDarkMode = snapshot.IsDarkMode;
        if (_isDarkMode != newIsDarkMode)
        {
            _isDarkMode = newIsDarkMode;
            Dispatcher.UIThread.Post(() => UpdateTheme());
        }
    }

    private void UpdateTheme()
    {
        _isDarkMode = ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        if (_isDarkMode)
        {
            RootBorder.Background = new SolidColorBrush(Color.Parse("#1B2129"));
            RootBorder.BorderBrush = new SolidColorBrush(Color.Parse("#2D3440"));
        }
        else
        {
            RootBorder.Background = new SolidColorBrush(Color.Parse("#FCFBFA"));
            RootBorder.BorderBrush = new SolidColorBrush(Color.Parse("#E8E8E8"));
        }
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await _viewModel.LoadAssignmentsAsync();
    }

    private async void OnAddClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await _viewModel.AddAssignmentAsync();
    }
}
