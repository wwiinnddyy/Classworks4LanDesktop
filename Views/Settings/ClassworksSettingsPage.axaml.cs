using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassworksPlugin.ViewModels.Settings;

namespace ClassworksPlugin.Views.Settings
{
    /// <summary>
    /// Code behind for <see cref="ClassworksSettingsPage"/>.  Handles button
    /// clicks and delegates to the view model where appropriate.
    /// </summary>
    public partial class ClassworksSettingsPage : UserControl
    {
        public ClassworksSettingsPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
}