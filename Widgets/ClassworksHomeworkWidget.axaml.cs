using ClassworksPlugin.Services;
using LanMountainDesktop.PluginSdk;

using Avalonia.Controls;

namespace ClassworksPlugin.Widgets;

public partial class ClassworksHomeworkWidget : UserControl
{
    private readonly ClassworksHomeworkViewModel _viewModel;

    public ClassworksHomeworkWidget(
        PluginDesktopComponentContext context,
        ClassworksSettingsService settingsService,
        ClassworksService classworksService) : this()
    {
        _ = context;
        _viewModel = new ClassworksHomeworkViewModel(settingsService, classworksService);
        DataContext = _viewModel;

        AttachedToVisualTree += async (_, _) =>
        {
            await _viewModel.LoadAssignmentsAsync();
        };
    }

    public ClassworksHomeworkWidget()
    {
        InitializeComponent();
        _viewModel = new ClassworksHomeworkViewModel(
            new ClassworksSettingsService(Path.Combine(Path.GetTempPath(), "classworks-design")),
            new ClassworksService());
        DataContext = _viewModel;
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
