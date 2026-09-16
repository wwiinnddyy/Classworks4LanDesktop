using ClassworksPlugin.Services;
using LanMountainDesktop.AirAppSdk;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia;
using Avalonia.VisualTree;

namespace ClassworksPlugin.Widgets;

public partial class ClassworksHomeworkWidget : UserControl
{
    private readonly ClassworksHomeworkViewModel _viewModel;
    private readonly IAirAppAppearanceContext? _appearance;
    private bool _isDarkMode;

    public ClassworksHomeworkWidget()
    {
        InitializeComponent();
        var designDataDirectory = Path.Combine(Path.GetTempPath(), "Classworks4LanDesktop", "Design");
        _viewModel = new ClassworksHomeworkViewModel(
            new ClassworksSettingsService(designDataDirectory),
            new ClassworksService());
        InitializeView();
    }

    public ClassworksHomeworkWidget(
        AirAppComponentContext context,
        ClassworksSettingsService settingsService,
        ClassworksService classworksService)
    {
        ArgumentNullException.ThrowIfNull(context);

        InitializeComponent();
        _appearance = context.Appearance;
        _viewModel = new ClassworksHomeworkViewModel(settingsService, classworksService);
        InitializeView();
    }

    private void InitializeView()
    {
        DataContext = _viewModel;

        ActualThemeVariantChanged += (_, _) => UpdateTheme();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
        ApplyAppearance();
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_appearance is not null)
        {
            _appearance.Changed -= OnAppearanceChanged;
            _appearance.Changed += OnAppearanceChanged;
        }

        ApplyAppearance();
        _ = _viewModel.LoadAssignmentsAsync();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_appearance is not null)
        {
            _appearance.Changed -= OnAppearanceChanged;
        }
    }

    private void OnAppearanceChanged(object? sender, AppearanceChangedEvent e)
    {
        Dispatcher.UIThread.Post(ApplyAppearance);
    }

    private void ApplyAppearance()
    {
        RootBorder.CornerRadius = _appearance is null
            ? new CornerRadius(12)
            : new CornerRadius(_appearance.ResolveCornerRadius(AirAppCornerRadiusPreset.Component));
        UpdateTheme();
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
