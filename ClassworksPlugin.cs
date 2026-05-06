using System;
using System.Threading.Tasks;
using Avalonia.Controls;
// The following using statements refer to types defined in the LanMountainDesktop
// plugin SDK.  They will resolve correctly when you build this project inside
// the LanMountainDesktop solution.  If you are building outside the host
// solution you may need to add stubs for these types.
using LanMountainDesktop.PluginSdk;
using LanMountainDesktop.Shared.Contracts;

namespace ClassworksPlugin
{
    /// <summary>
    /// Entry point for the Classworks plugin.  The host will create an
    /// instance of this class based on the entry point specified in
    /// <c>plugin.manifest.json</c>.  The plugin registers one widget
    /// component that displays assignments from the Classworks platform and
    /// exposes a simple settings page for login configuration.
    /// </summary>
    public sealed class ClassworksPlugin : WidgetPluginBase
    {
        /// <summary>
        /// Gets the unique identifier for this plugin.  This value must
        /// match the <c>id</c> property in your manifest file.
        /// </summary>
        public override string Id => "com.example.classworkswidget";

        /// <summary>
        /// Gets the display name of the plugin shown in the plugin manager.
        /// </summary>
        public override string Name => "Classworks 作业组件";

        /// <summary>
        /// Gets a brief description of the plugin.  This is shown in the
        /// plugin manager UI.
        /// </summary>
        public override string Description =>
            "在阑山桌面上显示来自 Classworks 的作业列表，并支持通过厚浪云账号登录。";

        /// <summary>
        /// Gets the current version of the plugin.  Update this value when
        /// releasing new versions.
        /// </summary>
        public override Version Version => new Version(0, 1, 0);

        /// <summary>
        /// This method is called by the host when the plugin is first
        /// initialised.  Perform any one‑time setup here.  For this plugin
        /// there is nothing to initialise ahead of time, so the method
        /// completes synchronously.
        /// </summary>
        public override Task InitialiseAsync()
        {
            // Nothing to do
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a new widget instance.  Each widget is a small view
        /// representing a component on the desktop.  The host calls this
        /// method whenever the user adds the widget to a desktop page.
        /// </summary>
        /// <returns>A widget component that displays assignments.</returns>
        public override IWidget CreateWidget()
        {
            return new ClassworksWidget();
        }

        /// <summary>
        /// Creates the plugin settings page.  Override this method to provide
        /// a custom settings view that will appear in the settings panel of
        /// the LanMountainDesktop host.  The settings page uses
        /// FluentAvalonia controls and allows the user to choose between
        /// entering credentials directly or launching a browser to obtain a
        /// token.
        /// </summary>
        /// <returns>A control representing the settings page.</returns>
        public override Control CreateSettingsView()
        {
            var vm = new ViewModels.Settings.ClassworksSettingsViewModel();
            var view = new Views.Settings.ClassworksSettingsPage
            {
                DataContext = vm
            };
            return view;
        }
    }
}