using ClassworksPlugin.Services;
using ClassworksPlugin.Views.Settings;
using LanMountainDesktop.AirAppSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ClassworksPlugin.Tests;

public sealed class PluginRegistrationTests
{
    [Fact]
    public void InitializeRegistersSettingsAndHomeworkComponent()
    {
        var services = new ServiceCollection();
        var plugin = new Plugin();

        plugin.Initialize(
            new HostBuilderContext(new Dictionary<object, object>()),
            services);

        var settingsRegistration = Assert.Single(
            services
                .Where(descriptor => descriptor.ServiceType == typeof(AirAppSettingsSectionRegistration))
                .Select(descriptor => Assert.IsType<AirAppSettingsSectionRegistration>(descriptor.ImplementationInstance)));
        Assert.Equal(ClassworksSettingsService.SectionId, settingsRegistration.Id);
        Assert.Equal(typeof(ClassworksSettingsPage), settingsRegistration.CustomViewType);

        var componentRegistration = Assert.Single(
            services
                .Where(descriptor => descriptor.ServiceType == typeof(AirAppComponentRegistration))
                .Select(descriptor => Assert.IsType<AirAppComponentRegistration>(descriptor.ImplementationInstance)));
        Assert.Equal("Classworks4LanDesktop.Homework", componentRegistration.ComponentId);
        Assert.True(componentRegistration.AllowDesktopPlacement);
        Assert.False(componentRegistration.AllowStatusBarPlacement);
        Assert.Equal(AirAppComponentResizeMode.Free, componentRegistration.ResizeMode);
        Assert.Equal(AirAppCornerRadiusPreset.Component, componentRegistration.CornerRadiusPreset);
    }
}
