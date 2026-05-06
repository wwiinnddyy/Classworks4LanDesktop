using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using LanMountainDesktop.PluginSdk;

namespace ClassworksPlugin
{
    /// <summary>
    /// Widget that displays a list of assignments retrieved from the
    /// Classworks service.  The widget uses a simple MVVM pattern where the
    /// view (defined in XAML) binds to an instance of <see cref="ClassworksViewModel"/>.
    /// </summary>
    public sealed class ClassworksWidget : WidgetBase
    {
        private readonly ClassworksViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassworksWidget"/> class.
        /// </summary>
        public ClassworksWidget()
        {
            // Create the view model and load assignments asynchronously.
            _viewModel = new ClassworksViewModel();
            // Kick off asynchronous loading but do not await it here to
            // prevent blocking the UI thread.  The view model handles
            // exceptions and updates the Status property accordingly.
            _ = _viewModel.LoadAssignmentsAsync();
        }

        /// <inheritdoc/>
        public override string Title => "作业";

        /// <inheritdoc/>
        public override Control BuildContent()
        {
            // The XAML view is compiled into a typed class by Avalonia.  We
            // instantiate it here and set its DataContext to the view model.
            var view = new Views.ClassworksWidgetView
            {
                DataContext = _viewModel
            };
            return view;
        }
    }
}