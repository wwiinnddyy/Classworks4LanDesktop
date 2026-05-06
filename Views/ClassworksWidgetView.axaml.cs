using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ClassworksPlugin.Views
{
    /// <summary>
    /// Code behind for <see cref="ClassworksWidgetView"/>.  Avalonia uses
    /// partial classes to connect XAML definitions to C# code.  This file
    /// simply calls <see cref="InitializeComponent"/> to load the XAML at
    /// runtime.
    /// </summary>
    public partial class ClassworksWidgetView : UserControl
    {
        public ClassworksWidgetView()
        {
            InitializeComponent();
        }

        private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ClassworksPlugin.ClassworksViewModel vm)
            {
                await vm.LoadAssignmentsAsync();
            }
        }

        private async void OnAddClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ClassworksPlugin.ClassworksViewModel vm)
            {
                await vm.AddAssignmentAsync();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}