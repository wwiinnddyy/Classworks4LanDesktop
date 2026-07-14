using ClassworksPlugin.Services;
using ClassworksPlugin.Views.Settings;
using LanMountainDesktop.PluginSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ClassworksPlugin.Tests;

public sealed class PluginRegistrationTests
{
    [Fact]
    public void InitializeRegistersApi5SettingsAndHomeworkComponent()
    {
        var services = new ServiceCollection();
        var plugin = new Plugin();

        plugin.Initialize(
            new HostBuilderContext(new Dictionary<object, object>()),
            services);

        var settingsRegistration = Assert.Single(
            services
                .Where(descriptor => descriptor.ServiceType == typeof(PluginSettingsSectionRegistration))
                .Select(descriptor => Assert.IsType<PluginSettingsSectionRegistration>(descriptor.ImplementationInstance)));
        Assert.Equal(ClassworksSettingsService.SectionId, settingsRegistration.Id);
        Assert.Equal(typeof(ClassworksSettingsPage), settingsRegistration.CustomViewType);

        var componentRegistration = Assert.Single(
            services
                .Where(descriptor => descriptor.ServiceType == typeof(PluginDesktopComponentRegistration))
                .Select(descriptor => Assert.IsType<PluginDesktopComponentRegistration>(descriptor.ImplementationInstance)));
        Assert.Equal("Classworks4LanDesktop.Homework", componentRegistration.ComponentId);
        Assert.True(componentRegistration.AllowDesktopPlacement);
        Assert.False(componentRegistration.AllowStatusBarPlacement);
        Assert.Equal(PluginDesktopComponentResizeMode.Free, componentRegistration.ResizeMode);
        Assert.Equal(PluginCornerRadiusPreset.Component, componentRegistration.CornerRadiusPreset);
    }
}
