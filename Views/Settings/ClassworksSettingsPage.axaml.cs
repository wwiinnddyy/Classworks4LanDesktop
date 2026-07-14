using ClassworksPlugin.ViewModels.Settings;
using LanMountainDesktop.PluginSdk;

namespace ClassworksPlugin.Views.Settings;

public partial class ClassworksSettingsPage : SettingsPageBase
{
    public ClassworksSettingsPage()
    {
        InitializeComponent();
    }

    public ClassworksSettingsPage(ClassworksSettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private async void OnSaveSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ClassworksSettingsViewModel vm)
        {
            await vm.SaveSettingsAsync();
        }
    }

    private async void OnBrowserLoginClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ClassworksSettingsViewModel vm)
        {
            await vm.BeginBrowserLoginAsync();
        }
    }
}
